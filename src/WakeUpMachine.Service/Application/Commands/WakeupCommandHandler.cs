using Microsoft.Extensions.Options;
using WakeUpMachine.Service.Maintenance;

namespace WakeUpMachine.Service.Application.Commands;

internal class WakeupCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IWakeupService _wakeupService;
    private readonly IPingService _pingService;
    private readonly INotificationService _notificationService;
    private readonly IOptions<WakeUpMachineServiceSettings> _options;

    public WakeupCommandHandler(IUserRepository userRepository, IWakeupService wakeupService, IPingService pingService,
        INotificationService notificationService, IOptions<WakeUpMachineServiceSettings> options)
    {
        _userRepository = userRepository;
        _wakeupService = wakeupService;
        _pingService = pingService;
        _notificationService = notificationService;
        _options = options;
    }

    public async Task Handle(WakeupCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(request.UserId, cancellationToken) ??
                   throw new InvalidOperationException("User not found.");

        if (user.AssignedMachine is null)
            throw new WakeUpMachineDomainException("There is no machine assigned to user. " +
                                                   "Please contact to admin to assign machine to the user first.");

        // Check if machine already turned on

        if (user.AssignedMachine.HostNameOrIpAddress is not null)
        {
            var pingBeforeOk =
                await _pingService.Ping(user.AssignedMachine.HostNameOrIpAddress, TimeSpan.FromSeconds(2));
            if (pingBeforeOk)
            {
                await _notificationService.Notify(user, $"{user.AssignedMachine.ToChatString()} already turned on!");
                return;
            }
        }

        // Wake machine up

        await _wakeupService.WakeUpMachine(user.AssignedMachine);
        await _notificationService.Notify(user,
            $"WakeOnLan packet sent to {user.AssignedMachine.ToChatString()}.\nWait for {_options.Value.PingTimeoutSec} sec..");

        // Wait until machine is switched on if IP or Host name specified.
        // TODO: Force specific IP and Host name in Machine it helps to avoid additional conditions

        if (user.AssignedMachine.HostNameOrIpAddress is null)
            return;

        await Task.Delay(TimeSpan.FromSeconds(_options.Value.PingTimeoutSec), cancellationToken);
        var pingOk = await _pingService.Ping(user.AssignedMachine.HostNameOrIpAddress, TimeSpan.FromSeconds(5));

        var reply = pingOk
            ? $"{user.AssignedMachine.ToChatString()} is ready to work!"
            : $"{user.AssignedMachine.ToChatString()} didn't reply on ICMP echo. Might be it's ready but firewall filter ICMP.";

        await _notificationService.Notify(user, reply);
    }
}