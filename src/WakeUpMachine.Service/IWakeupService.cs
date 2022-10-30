namespace WakeUpMachine.Service;

internal interface IWakeupService
{
    Task WakeUpMachine(Machine machine);
}