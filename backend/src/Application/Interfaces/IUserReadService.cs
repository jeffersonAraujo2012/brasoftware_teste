using Application.Users.DTOs;
using Application.Users.Queries.ListUsers;

namespace Application.Interfaces;

public interface IUserReadService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<UserDto> Items, int TotalItems)> ListAsync(
        int page, int pageSize, UserSortField sortField, SortDirection sortDirection, CancellationToken cancellationToken);
}
