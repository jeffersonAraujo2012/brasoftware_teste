using Domain.Users;
using Infra.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistence;

public sealed class UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
