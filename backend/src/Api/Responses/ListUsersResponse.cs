using Application.Users.DTOs;

namespace Api.Responses;

public sealed record ListUsersResponse(
    IReadOnlyList<UserResponse> Items, int Page, int PageSize, int TotalItems, int TotalPages)
{
    internal static ListUsersResponse From(UserPageDto page)
        => new(page.Items.Select(UserResponse.From).ToArray(), page.Page,
            page.PageSize, page.TotalItems, page.TotalPages);
}
