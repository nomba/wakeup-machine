namespace WakeUpMachine.Service;

internal interface IUserRepository
{
    Task<User?> GetById(long userId, CancellationToken cancellationToken);
    Task<User> Add(User user, CancellationToken cancellationToken);
    Task Update(User user, CancellationToken cancellationToken);
}