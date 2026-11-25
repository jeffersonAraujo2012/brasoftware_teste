using System.Linq.Expressions;
using Application.Abstractions;
using Application.Users;
using Domain.Users;
using Infra.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infra.Persistence.Repositories;

internal sealed class UserRepository(UserManagementDbContext database) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => database.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> ListAsync(
        int page, int pageSize, UserSortField sortField, SortDirection sortDirection, CancellationToken cancellationToken)
    {
        var users = database.Users.AsNoTracking();
        var totalItems = await users.CountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        if (offset >= totalItems)
        {
            return ([], totalItems);
        }

        var ordered = sortField switch
        {
            UserSortField.FullName => SortOn(users, user => user.FullName, sortDirection),
            UserSortField.Email => SortOn(users, user => user.Email, sortDirection),
            UserSortField.Cpf => SortOn(users, user => user.Cpf, sortDirection),
            UserSortField.CreatedAt => SortOn(users, user => user.CreatedAt, sortDirection),
            UserSortField.Status => SortOn(users, user => user.Status, sortDirection),
            _ => throw new ArgumentOutOfRangeException(nameof(sortField))
        };

        var items = await ordered.ThenBy(user => user.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalItems);
    }

    private static IOrderedQueryable<User> SortOn<TKey>(
        IQueryable<User> users, Expression<Func<User, TKey>> key, SortDirection direction)
        => direction == SortDirection.Asc ? users.OrderBy(key) : users.OrderByDescending(key);

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
