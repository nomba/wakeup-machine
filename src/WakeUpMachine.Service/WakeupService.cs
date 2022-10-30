using System.Net.NetworkInformation;
using System.Net;

namespace WakeUpMachine.Service;

internal class WakeupService : IWakeupService
{
    private readonly ILogger<WakeupService> _logger;

    public WakeupService(ILogger<WakeupService> logger)
    {
        _logger = logger;
    }

    public async Task WakeUpMachine(Machine machine)
    {
        if (!PhysicalAddress.TryParse(machine.Mac, out var macAddress))
            throw new InvalidOperationException("Unable to parse MAC address.");

        // If machine specified with IP and Net mask then send specific net broadcast e.g. 192.168.0.255

        if (machine.Ip is not null && machine.NetMask is not null)
        {
            var netBroadcast = GetBroadcastAddress(IPAddress.Parse(machine.Ip), IPAddress.Parse(machine.NetMask));
            _logger.LogDebug("Sending WOL for {MacAddress} to {NetBroadcast}", macAddress, netBroadcast);

            await macAddress.SendWolAsync(netBroadcast);
            return;
        }

        // Otherwise send broadcast request to any net 255.255.255.255
        
        _logger.LogDebug("Sending WOL for {MacAddress} to {NetBroadcast}", macAddress, IPAddress.Broadcast);
        await macAddress.SendWolAsync();
    }

    private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
    {
        var ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
        var ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
        var broadCastIpAddress = ipAddress | ~ipMaskV4;

        return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
    }
}