using Application.Interfaces;
using Application.Users.DTOs;
using MediatR;

namespace Application.Users.Queries.ListUsers;

internal sealed class ListUsersQueryHandler(IUserReadService readService)
    : IRequestHandler<ListUsersQuery, UserPageDto>
{
    public async Task<UserPageDto> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalItems) = await readService.ListAsync(request.Page, request.PageSize,
            ListUsersQuery.SortFields[request.SortBy!], ListUsersQuery.SortDirections[request.SortDirection!],
            cancellationToken);
        var totalPages = totalItems == 0 ? 0 : 1 + (totalItems - 1) / request.PageSize;

        return new UserPageDto(items, request.Page, request.PageSize, totalItems, totalPages);
    }
}
