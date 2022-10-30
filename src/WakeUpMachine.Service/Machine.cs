namespace WakeUpMachine.Service;

internal class Machine
{
    public Machine(string mac, string? ip, string? hostName)
    {
        Mac = mac;
        Ip = ip;
        HostName = hostName;
    }

    public long Id { get; set; }
    public string Mac { get; set; }
    public string? Ip { get; set; }
    public string? NetMask { get; set; }
    public string? HostName { get; set; }

    public string? HostNameOrIpAddress => Ip ?? HostName;
}