using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WakeUpMachine.Service;

public class WakeUpMachineWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WakeUpMachineWorker> _logger;
    private readonly ITelegramBotClient _telegramBotClient;

    public WakeUpMachineWorker(IServiceProvider serviceProvider, ILogger<WakeUpMachineWorker> logger,
        ITelegramBotClient telegramBotClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _telegramBotClient = telegramBotClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var botUpdateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();

            var receiverOptions = new ReceiverOptions
            {
                // Receive all update types
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _telegramBotClient.StartReceiving(botUpdateHandler, receiverOptions, stoppingToken);
            _logger.LogInformation("Bot listening is started");

            // Leave this block to provide scope for IUpdateHandler because it uses DbContext that requires scoped lifetime
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug($"GracePeriod task doing background work.");
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"Worker stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            Environment.Exit(1);
        }
    }
}