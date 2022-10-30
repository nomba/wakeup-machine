namespace WakeUpMachine.Service;

internal interface INotificationService
{
    Task Notify(User user, string message);
}