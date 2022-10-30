using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using WakeUpMachine.Service;
using WakeUpMachine.Service.Configuring;
using WakeUpMachine.Service.Infrastructure;

// Build host

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .UseWindowsService(options => options.ServiceName = "WakeUpMachine")
    .ConfigureLogging((context, logging) => { logging.AddConfiguration(context.Configuration.GetSection("Logging")); })
    .ConfigureServices((context, services) =>
    {
        // Bind WakeUpMachineServiceSettings with appsettings.json section
        services.Configure<WakeUpMachineServiceSettings>(
            context.Configuration.GetSection(WakeUpMachineServiceSettings.SectionName));
        
        // Register background worker
        services.AddHostedService<WakeUpMachineWorker>();
        
        // Telegram client
        services.AddTransient<ITelegramBotClient>(_ =>
        {
            var options = _.GetRequiredService<IOptions<WakeUpMachineServiceSettings>>();
            return new TelegramBotClient(options.Value.BotToken);
        });
        
        // Stateless
        services.AddTransient<IWakeupService, WakeupService>();
        services.AddTransient<IPingService, PingService>();
        services.AddTransient<INotificationService, NotificationService>();
        services.AddTransient<IAssignmentService, AssignmentService>();

        // DbContext related. Needs be scoped.
        services.AddScoped<IUpdateHandler, TelegramBotUpdateHandler>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddDbContext<WakeUpMachineContext>(opt => { opt.UseSqlite(context.Configuration.GetConnectionString("Default")); });

        // Configurator
        services.AddScoped<WakeUpMachineServiceConfigurator>();
    })
    .Build();

// Ensure DB created

await using (var scope = host.Services.CreateAsyncScope())
{
    var wakeUpMachineContext = scope.ServiceProvider.GetRequiredService<WakeUpMachineContext>();
    wakeUpMachineContext.Database.EnsureCreated();
}

// Run once if '--configure' specified

if (args.Contains("--configure"))
{
    await using var scope = host.Services.CreateAsyncScope();
    var serviceConfigurator = scope.ServiceProvider.GetRequiredService<WakeUpMachineServiceConfigurator>();

    // Simple parse bot token value
    var botTokenArg = args.FirstOrDefault(_ => _.StartsWith("--bottoken="));
    var botToken = botTokenArg?.Split("=", 2)[1];

    await serviceConfigurator.Configure(botToken);
    return;
}

// Check if bot works

await using (var scope = host.Services.CreateAsyncScope())
{
    var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await botClient.TestApiAsync();
    }
    catch (Exception ex)
    {
        logger.LogCritical("Bot does not work: {Message}", ex);
        Environment.Exit(1);
    }
}

// Run in background
await host.RunAsync();