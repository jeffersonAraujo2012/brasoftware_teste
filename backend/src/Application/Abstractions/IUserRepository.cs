using Application.Users;
using Domain.Users;

namespace Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<User> Items, int TotalItems)> ListAsync(
        int page, int pageSize, UserSortField sortField, SortDirection sortDirection, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);
    Task DeleteAsync(User user, CancellationToken cancellationToken);
}
