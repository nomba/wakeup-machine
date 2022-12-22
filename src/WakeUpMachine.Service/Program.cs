using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using WakeUpMachine.Service;
using WakeUpMachine.Service.Infrastructure;
using WakeUpMachine.Service.Maintenance;

// Overrides default CurrentDirectory to executable folder path
Environment.CurrentDirectory = AppContext.BaseDirectory;

// Build host

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .UseWindowsService(options => options.ServiceName = "WakeUpMachine")
    .UseSerilog((context, configuration) => { configuration.WriteTo.Console().ReadFrom.Configuration(context.Configuration);})
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

        // Configurators
        services.AddScoped<ConnectionStringConfigurator>();
        services.AddScoped<WakeUpMachineServiceConfigurator>();
        
        // Backup
        services.AddScoped<BackupCreator>();
    })
    .Build();

// `configure` command
// Provides initial configuring and further configuring by administrator  

if (args.Length >= 1 && args[0] == "configure")
{
    await using var scope = host.Services.CreateAsyncScope();

    // Firstly, configure initial (empty before) connection string. If CS is already set then skip configuring
    var connectionStringConfigurator = scope.ServiceProvider.GetRequiredService<ConnectionStringConfigurator>();

    var dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WakeUpMachine");
    var connectionString = $"Data source={Path.Combine(dbFolder, "WakeUpMachine.db")}";
    await connectionStringConfigurator.Configure(connectionString, false);

    if (!Directory.Exists(dbFolder))
        Directory.CreateDirectory(dbFolder);

    // Ensure database is created
    var wakeUpMachineContext = scope.ServiceProvider.GetRequiredService<WakeUpMachineContext>();
    wakeUpMachineContext.Database.EnsureCreated();

    // Secondly, any settings
    var serviceConfigurator = scope.ServiceProvider.GetRequiredService<WakeUpMachineServiceConfigurator>();

    // Simple parse bot token value
    var botTokenArg = args.FirstOrDefault(_ => _.StartsWith("--bot-token="));
    var botToken = botTokenArg?.Split("=", 2)[1];

    await serviceConfigurator.Configure(botToken);
    return;
}

// `backup` command 
// Needs do backup database file and appSettings.json

if (args.Length >= 1 && args[0] == "backup")
{
    await using var scope = host.Services.CreateAsyncScope();
    var backupCreator = scope.ServiceProvider.GetRequiredService<BackupCreator>();

    await backupCreator.Backup();
    return;
}

// Ensure DB created

await using (var scope = host.Services.CreateAsyncScope())
{
    var wakeUpMachineContext = scope.ServiceProvider.GetRequiredService<WakeUpMachineContext>();
    wakeUpMachineContext.Database.EnsureCreated();
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