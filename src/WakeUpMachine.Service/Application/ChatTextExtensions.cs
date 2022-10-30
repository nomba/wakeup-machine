namespace WakeUpMachine.Service.Application;

internal static class ChatTextExtensions
{
    public static string ToChatString(this Machine machine)
    {
        var machineChatString = machine.Mac;

        if (machine.Ip is not null)
            machineChatString = $"{machine.Ip} ({machine.Mac})";

        if (machine.HostName is not null)
            machineChatString = $"{machine.HostName} ({machine.Mac})";

        return machineChatString;
    }
}