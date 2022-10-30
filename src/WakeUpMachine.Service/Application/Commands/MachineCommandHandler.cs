namespace WakeUpMachine.Service.Application.Commands;

internal class MachineCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPingService _pingService;
    private readonly INotificationService _notificationService;

    public MachineCommandHandler(IUserRepository userRepository, IPingService pingService,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _pingService = pingService;
        _notificationService = notificationService;
    }

    public async Task Handle(MachineCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(request.UserId, cancellationToken) ??
                   throw new InvalidOperationException("User not found.");

        if (user.AssignedMachine is null)
            throw new WakeUpMachineDomainException("There is no machine assigned to user. " +
                                                   "Please contact to admin to assign machine to the user first.");

        bool? isOnline = null;

        if (user.AssignedMachine.HostNameOrIpAddress is not null)
            isOnline = await _pingService.Ping(user.AssignedMachine.HostNameOrIpAddress, TimeSpan.FromSeconds(3));

        var status = isOnline.HasValue ? isOnline.Value ? "Online" : "Off" : "Unknown";
        var reply = $"Assigned machine: {user.AssignedMachine.ToChatString()}.\nStatus: {status}";

        await _notificationService.Notify(user, reply);
    }
}