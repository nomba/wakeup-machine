namespace WakeUpMachine.Service;

public class WakeUpMachineDomainException : Exception
{
    public WakeUpMachineDomainException()
    {
    }

    public WakeUpMachineDomainException(string message) : base(message)
    {
    }

    public WakeUpMachineDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}