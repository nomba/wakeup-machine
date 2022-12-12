using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace WakeUpMachine.Service.Configuring;

internal class WakeUpMachineServiceConfigurator
{
    private readonly ILogger<WakeUpMachineServiceConfigurator> _logger;
    private readonly IOptions<WakeUpMachineServiceSettings> _options;
    private readonly IUserRepository _userRepository;
    private readonly IAssignmentService _assignmentService;

    public WakeUpMachineServiceConfigurator(ILogger<WakeUpMachineServiceConfigurator> logger,
        IOptions<WakeUpMachineServiceSettings> options,
        IUserRepository userRepository, IAssignmentService assignmentService)
    {
        _logger = logger;
        _options = options;
        _userRepository = userRepository;
        _assignmentService = assignmentService;
    }

    public async Task Configure(string? botToken)
    {
        _logger.LogInformation("Configuring started..");

        // Bot token

        if (botToken is not null)
            await SetBotToken(botToken);
        else
            _logger.LogInformation("Skip setting bot token");

        // Check if settings section in config is empty 

        if (_options.Value is null)
        {
            _logger.LogWarning("Config is empty. Skip configuring");
            return;
        }

        // Users

        if (_options.Value?.Users is null)
        {
            _logger.LogWarning("Users section in config is empty. Skip configuring users");
            return;
        }

        foreach (var userConfig in _options.Value.Users)
        {
            // Add user from settings, if the user already exists then skip it.

            if (userConfig.TelegramUserId <= 0)
            {
                _logger.LogWarning($"User must have {nameof(userConfig.TelegramUserId)}. It will be skipped.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(userConfig.FullName))
            {
                _logger.LogWarning($"User must have {nameof(userConfig.FullName)}. It will be skipped.");
                continue;
            }

            // Ensure user is into DB

            var user = await _userRepository.GetById(userConfig.TelegramUserId, CancellationToken.None);

            if (user is null)
            {
                user = await _userRepository.Add(
                    new User { Id = userConfig.TelegramUserId, FullName = userConfig.FullName }, CancellationToken.None);
                _logger.LogInformation(
                    "New user {NewUserId} ({NewUserFullName}) added to DB ", user.Id, user.FullName);
            }
            else if (user.AssignedMachine is not null && !_options.Value.ReassignUserMachine)
            {
                _logger.LogWarning(
                    "Skip configuring user {NewUserId} ({NewUserFullName}) machine. It's already assigned. " +
                    "Use {ReassignUserMachine}=true to rewrite already assigned machine",
                    user.Id,
                    user.FullName, nameof(_options.Value.ReassignUserMachine));

                continue;
            }

            // Check if machine is not assigned in config

            if (userConfig.Machine is null)
            {
                _logger.LogWarning("User {UserConfigTelegramUserId} ({UserConfigFullName})\'s machine is not set",
                    userConfig.TelegramUserId, userConfig.FullName);
                continue;
            }

            // Machine can be specified by only one option (in order of priority): Host name or IP or MAC

            MachineAddress machineAddress;

            if (userConfig.Machine.HostName is not null)
                machineAddress = MachineAddress.FromHostName(userConfig.Machine.HostName, userConfig.Machine.Mac);
            else if (userConfig.Machine.Ip is not null)
                machineAddress = MachineAddress.FromIp(userConfig.Machine.Ip, userConfig.Machine.Mac);
            else if (userConfig.Machine.Mac is not null)
                machineAddress = MachineAddress.FromMac(userConfig.Machine.Mac);
            else
            {
                _logger.LogWarning(
                    "User {UserConfigTelegramUserId} ({UserConfigFullName})\'s machine for must be specified by only one option: Host name, IP or MAC",
                    userConfig.TelegramUserId, userConfig.FullName);
                continue;
            }

            //

            try
            {
                await _assignmentService.AssignMachineToUser(user!, machineAddress, CancellationToken.None);
                _logger.LogInformation(
                    "Machine {Machine} was assigned to user {UserConfigTelegramUserId} ({UserConfigFullName})",
                    user!.AssignedMachine!.HostNameOrIpAddress, userConfig.TelegramUserId, userConfig.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Assigning machine to user {UserConfigTelegramUserId} ({UserConfigFullName}) failed",
                    userConfig.TelegramUserId, userConfig.FullName);
            }
        }

        _logger.LogInformation("Configuring finished");
    }

    private async Task SetBotToken(string botToken)
    {
        var appSettingsJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        var appSettingsJson = JsonNode.Parse(await File.ReadAllTextAsync(appSettingsJsonPath));
        if (appSettingsJson == null)
            throw new InvalidOperationException("Unable to parse appsettings.json.");
        
        // Ensure settings section is created
        appSettingsJson[WakeUpMachineServiceSettings.SectionName] ??= new JsonObject();

        // Update bot token
        appSettingsJson[WakeUpMachineServiceSettings.SectionName]!["BotToken"] = botToken;

        await File.WriteAllTextAsync(appSettingsJsonPath,
            appSettingsJson.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
                // Fix problem with "<>" chars, https://stackoverflow.com/a/58003397
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));

        _logger.LogInformation("Bot token in {AppSettingsJsonPath} updated", appSettingsJsonPath);
    }
}