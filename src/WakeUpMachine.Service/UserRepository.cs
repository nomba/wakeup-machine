using System.Collections.Concurrent;
using WakeUpMachine.Service.Infrastructure;

namespace WakeUpMachine.Service;

internal class UserRepository : IUserRepository
{
    private readonly WakeUpMachineContext _context;

    public UserRepository(WakeUpMachineContext context)
    {
        _context = context;
    }

    public async Task<User?> GetById(long userId, CancellationToken cancellationToken)
    {
        return await _context.Set<User>().FindAsync(new object[] {userId}, cancellationToken: cancellationToken);
    }

    public async Task<User> Add(User user, CancellationToken cancellationToken)
    {
        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task Update(User user, CancellationToken cancellationToken)
    {
        _context.Set<User>().Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}