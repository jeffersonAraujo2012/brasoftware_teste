using Application.Users;

namespace Api.Contracts;

public sealed record ListUsersResponse(
    IReadOnlyList<UserResponse> Items, int Page, int PageSize, int TotalItems, int TotalPages)
{
    internal static ListUsersResponse From(ListUsersResult result)
        => new(result.Items.Select(UserResponse.From).ToArray(), result.Page,
            result.PageSize, result.TotalItems, result.TotalPages);
}
