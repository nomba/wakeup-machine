using System.Net.NetworkInformation;

namespace WakeUpMachine.Service;

internal interface IPingService
{
    Task<bool> Ping(string hostNameOrIpAddress, TimeSpan timeout);
}

internal class PingService : IPingService
{
    private readonly ILogger<PingService> _logger;

    public PingService(ILogger<PingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> Ping(string hostNameOrIpAddress, TimeSpan timeout)
    {
        using var ping = new Ping();

        try
        {
            var reply = await ping.SendPingAsync(hostNameOrIpAddress, (int) timeout.TotalMilliseconds);
            return reply.Status == IPStatus.Success;
        }
        catch (PingException ex)
        {
            _logger.LogDebug(ex, "Ping exception on {HostNameOrIpAddress}", hostNameOrIpAddress);
            return false;
        }
    }
}