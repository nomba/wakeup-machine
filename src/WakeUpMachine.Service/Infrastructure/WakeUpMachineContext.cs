using Microsoft.EntityFrameworkCore;

namespace WakeUpMachine.Service.Infrastructure;

internal class WakeUpMachineContext : DbContext
{
    public WakeUpMachineContext(DbContextOptions<WakeUpMachineContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users

        modelBuilder.Entity<User>(userBuilder =>
        {
            userBuilder.HasKey(user => user.Id);
            userBuilder.OwnsOne(user => user.AssignedMachine);
        });
    }
}