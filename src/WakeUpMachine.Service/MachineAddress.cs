namespace WakeUpMachine.Service;

internal class MachineAddress
{
    private MachineAddress(MachineAddressType type, string? hostName, string? ip, string? mac)
    {
        Type = type;
        HostName = hostName;
        Ip = ip;
        Mac = mac;

        if (hostName is null && ip is null && mac is null)
            throw new ArgumentException("At least one parameter must be specified.");
    }

    public static MachineAddress FromMac(string macAddress)
    {
        return new MachineAddress(MachineAddressType.Mac, null, null, macAddress);
    }  
    
    public static MachineAddress FromIp(string ipAddress, string? macAddress = null)
    {
        return new MachineAddress(MachineAddressType.Ip, null, ipAddress, macAddress);
    }    
    
    public static MachineAddress FromHostName(string hostName, string? macAddress = null)
    {
        return new MachineAddress(MachineAddressType.HostName, hostName, null, macAddress);
    }

    public string? HostName { get; }
    public string? Ip { get; }
    public string? Mac { get; }

    public MachineAddressType Type { get; }
}

public enum MachineAddressType
{
    Mac,
    Ip,
    HostName
}