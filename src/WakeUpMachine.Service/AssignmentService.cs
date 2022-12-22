using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;
using WakeUpMachine.Service.Maintenance;

namespace WakeUpMachine.Service;

internal class AssignmentService : IAssignmentService
{
    private readonly ILogger<AssignmentService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IOptions<WakeUpMachineServiceSettings> _options;

    public AssignmentService(ILogger<AssignmentService> logger, IUserRepository userRepository,
        IOptions<WakeUpMachineServiceSettings> options)
    {
        _logger = logger;
        _userRepository = userRepository;
        _options = options;
    }

    public async Task AssignMachineToUser(User user, MachineAddress machineAddress, CancellationToken cancellationToken)
    {
        var previousMachine = user.AssignedMachine;
        var machine = await CreateMachine(machineAddress);

        user.AssignedMachine = machine;
        await _userRepository.Update(user, cancellationToken);

        if (previousMachine is not null)
        {
            _logger.LogInformation("A new machine {Machine} reassigned to user {UserId}({UserName})",
                machine.Mac, user.Id, user.Name);
        }
    }

    private async Task<Machine> CreateMachine(MachineAddress machineAddress)
    {
        // Fill MAC, IP, Host name 

        PhysicalAddress? macAddress;
        IPAddress? ipAddress = null;
        string? hostName = null;

        switch (machineAddress.Type)
        {
            case MachineAddressType.Mac:
                macAddress = PhysicalAddress.Parse(machineAddress.Mac);
                break;

            case MachineAddressType.Ip:
            {
                ipAddress = IPAddress.Parse(machineAddress.Ip);
                macAddress = await RetrieveMacAddressByIp(ipAddress);

                // If retrieving Mac fails, try use specified MAC
                if (macAddress is null && machineAddress.Mac is not null)
                {
                    _logger.LogWarning("Cannot retrieve MAC by IP {IpAddress}. Use specified one {MacAddress}",
                        ipAddress, machineAddress.Mac);
                    macAddress = PhysicalAddress.Parse(machineAddress.Mac);
                }

                if (machineAddress.Mac is not null)
                {
                    _logger.LogWarning(
                        "Ignore specified MAC {SpecifiedMacAddress} for IP {IpAddress}. Use just detected one: {DetectedMacAddress}",
                        machineAddress.Mac, ipAddress, macAddress);
                }

                break;
            }

            // case MachineAddressType.HostName:
            //     hostHame = machineAddress.HostName;
            //     ipAddress = await RetrieveIpAddressByHostName(machineAddress.HostName);
            //     macAddress = await RetrieveMacAddressByIp(ipAddress);
            //     break;

            case MachineAddressType.HostName:
                throw new NotImplementedException("Detecting MAC by host name currently not implemented.");

            default:
                throw new ArgumentOutOfRangeException();
        }

        if (macAddress is null)
            throw new WakeUpMachineDomainException("MAC address can't be detected. Please specify it manually.");

        if (ipAddress is null)
            _logger.LogWarning("IP address can\'t be detected for {MacAddress}", macAddress);

        // if (hostHame is null)
        //     _logger.LogWarning("Host name can\'t be detected for {MacAddress}", macAddress);

        return new Machine(macAddress.ToString(), ipAddress?.ToString(), hostName)
            {NetMask = _options.Value.DefaultNetMask};
    }

    private async Task<PhysicalAddress?> RetrieveMacAddressByIp(IPAddress ipAddress)
    {
        var arpResult = await ArpRequest.SendAsync(ipAddress);

        if (arpResult.Exception is null)
            return arpResult.Address;

        _logger.LogWarning("ARP error occurred { ExceptionMessage}", arpResult.Exception.Message);
        return null;
    }

    private async Task<IPAddress> RetrieveIpAddressByHostName(string hostName)
    {
        var addressList = await Dns.GetHostAddressesAsync(hostName);

        // Take first IP
        return addressList[0];
    }
}