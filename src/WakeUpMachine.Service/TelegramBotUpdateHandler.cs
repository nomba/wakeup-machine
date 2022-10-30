using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WakeUpMachine.Service.Application.Commands;
using WakeUpMachine.Service.Configuring;

namespace WakeUpMachine.Service;

internal class TelegramBotUpdateHandler : IUpdateHandler
{
    private readonly ILogger<TelegramBotUpdateHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IWakeupService _wakeupService;
    private readonly IPingService _pingService;
    private readonly INotificationService _notificationService;
    private readonly IOptions<WakeUpMachineServiceSettings> _options;

    public TelegramBotUpdateHandler(ILogger<TelegramBotUpdateHandler> logger, IUserRepository userRepository,
        IWakeupService wakeupService, IPingService pingService, INotificationService notificationService,
        IOptions<WakeUpMachineServiceSettings> options)
    {
        _logger = logger;
        _userRepository = userRepository;
        _wakeupService = wakeupService;
        _pingService = pingService;
        _notificationService = notificationService;
        _options = options;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => OnMessageReceived(botClient, update.Message, cancellationToken),
            _ => UnknownUpdateHandlerAsync(botClient, update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await OnHandlerExecutionFailed(botClient, update, exception, cancellationToken);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(errorMessage, exception);
        return Task.CompletedTask;
    }

    private async Task OnHandlerExecutionFailed(ITelegramBotClient botClient, Update problematicUpdate,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log exception

        _logger.LogError(exception, message: null);

        // Try reply with details

        var chatId = problematicUpdate.Message?.Chat.Id;

        if (chatId is null)
            return;

        await TryReply(botClient, chatId.Value, $"Problem with handling request data: {exception.Message}",
            cancellationToken);
    }

    private async Task OnMessageReceived(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var isCommandMessage = message.Type == MessageType.Text && message.Text?.StartsWith('/') == true;

        CommandType? command = message.Text?.Split(' ').FirstOrDefault() switch
        {
            "/machine" => CommandType.Machine,
            "/wakeup" => CommandType.WakeUp,
            _ => null
        };

        // Allow service only for authorized users

        var userId = message.From?.Id ??
                     throw new InvalidOperationException("Message does not contain user identifier");

        var user = await _userRepository.GetById(userId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Unauthorized user request: {FromId} ({FromUsername})", message.From?.Id,
                message.From?.Username);

            await botClient.SendTextMessageAsync(message.Chat.Id,
                $"User {userId} is not authorized. Please contact to administrator to allow using the service.",
                cancellationToken: cancellationToken);

            return;
        }

        // Update Chat ID

        if (user.ChatId is null || user.ChatId != message.Chat.Id)
        {
            user.ChatId = message.Chat.Id;
            await _userRepository.Update(user, cancellationToken);
            _logger.LogDebug("New Chat ID {ChatId} is used for user {UserId}", message.Chat.Id, user.Id);
        }

        // Check if command detected

        if (!isCommandMessage || command is null)
        {
            await AnswerWithAvailableCommands(botClient, message);
            return;
        }

        // Route command to handlers
        // TODO: Replace with MediatR

        var commandHandler = command switch
        {
            CommandType.WakeUp => new WakeupCommandHandler(_userRepository, _wakeupService, _pingService,
                    _notificationService, _options)
                .Handle(new WakeupCommand {UserId = user.Id}, cancellationToken),

            CommandType.Machine => new MachineCommandHandler(_userRepository, _pingService, _notificationService)
                .Handle(new MachineCommand {UserId = user.Id}, cancellationToken),

            _ => Task.FromException(new NotImplementedException($"{command} command is not implemented yet."))
        };

        try
        {
            await commandHandler;
        }
        catch (WakeUpMachineDomainException domainException)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, domainException.Message,
                cancellationToken: cancellationToken);
        }
    }

    private static async Task AnswerWithAvailableCommands(ITelegramBotClient botClient, Message message)
    {
        const string usage = "Usage:\n" +
                             "/machine - get the machine assigned to current telegram user\n" +
                             "/wakeup - send request to wake up the assigned machine\n";

        await botClient.SendTextMessageAsync(message.Chat.Id, usage);
    }

    private async Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
    {
        _logger.LogWarning("Unknown update type: {UpdateType}", update.Type);

        // Try to reply on unknown update type

        var chatId = update.Message?.Chat.Id;

        if (chatId is null)
            return;

        await TryReply(botClient, chatId.Value, "Unknown send data type", CancellationToken.None);
    }

    private async Task<bool> TryReply(ITelegramBotClient botClient, long chatId, string message,
        CancellationToken cancellationToken)
    {
        try
        {
            // Sometimes bot can not reply due to permission issues. In this case we have to protect sending method
            await botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Can not reply to {ChatId} with message: {Message}", chatId, message);
            return false;
        }
    }
}