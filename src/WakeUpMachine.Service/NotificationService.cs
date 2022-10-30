using Telegram.Bot;

namespace WakeUpMachine.Service;

internal class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly ITelegramBotClient _botClient;

    public NotificationService(ILogger<NotificationService> logger, ITelegramBotClient botClient)
    {
        _logger = logger;
        _botClient = botClient;
    }

    public async Task Notify(User user, string message)
    {
        if (user.ChatId is null)
            _logger.LogWarning("User {UserName} ({UserId}) does not have chat ID", user.Name, user.Id);
        else
            await _botClient.SendTextMessageAsync(user.ChatId!.Value, message);
    }
}