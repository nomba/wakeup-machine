namespace WakeUpMachine.Service.Configuring;

internal class WakeUpMachineServiceSettings
{
    public const string SectionName = "Settings";
    public string BotToken { get; set; }
    public bool ReassignUserMachine { get; set; }
    public string DefaultNetMask { get; set; } = "255.255.255.0";
    public byte PingTimeoutSec { get; set; } = 10;
    public UserConfig[] Users { get; set; }
}