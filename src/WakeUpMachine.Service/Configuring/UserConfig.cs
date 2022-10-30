namespace WakeUpMachine.Service.Configuring;

internal class UserConfig
{
    public long TelegramUserId { get; set; }
    public string FullName { get; set; }
    public MachineConfig? Machine { get; set; }
}