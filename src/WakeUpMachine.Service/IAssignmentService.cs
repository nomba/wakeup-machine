namespace WakeUpMachine.Service;

internal interface IAssignmentService
{
    Task AssignMachineToUser(User user, MachineAddress machineAddress, CancellationToken cancellationToken);
}