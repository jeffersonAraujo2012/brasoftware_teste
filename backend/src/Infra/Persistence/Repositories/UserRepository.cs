using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Infra.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infra.Persistence.Repositories;

internal sealed class UserRepository(UserManagementDbContext database) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => database.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        database.Users.Add(user);
        return SaveAsync(user.Id, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
        => SaveAsync(user.Id, cancellationToken);

    public Task DeleteAsync(User user, CancellationToken cancellationToken)
    {
        database.Users.Remove(user);
        return SaveAsync(user.Id, cancellationToken);
    }

    private async Task SaveAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new UserNotFoundException(userId, exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UserConfiguration.EmailIndex or UserConfiguration.CpfIndex
        } postgres)
        {
            var field = postgres.ConstraintName == UserConfiguration.EmailIndex ? "email" : "cpf";
            throw new UserConflictException(field, exception);
        }
    }
}
