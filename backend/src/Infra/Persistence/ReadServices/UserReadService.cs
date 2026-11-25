using System.Linq.Expressions;
using Application.Interfaces;
using Application.Users.DTOs;
using Application.Users.Queries.ListUsers;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistence.ReadServices;

internal sealed class UserReadService(UserManagementDbContext database) : IUserReadService
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await database.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
        return user is null ? null : UserDto.FromEntity(user);
    }

    public async Task<(IReadOnlyList<UserDto> Items, int TotalItems)> ListAsync(
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
        return (items.Select(UserDto.FromEntity).ToArray(), totalItems);
    }

    private static IOrderedQueryable<User> SortOn<TKey>(
        IQueryable<User> users, Expression<Func<User, TKey>> key, SortDirection direction)
        => direction == SortDirection.Asc ? users.OrderBy(key) : users.OrderByDescending(key);
}
