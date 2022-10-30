namespace WakeUpMachine.Service;

internal class User
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? FullName { get; set; }

    public long? ChatId { get; set; }
    public Machine? AssignedMachine { get; set; }
}