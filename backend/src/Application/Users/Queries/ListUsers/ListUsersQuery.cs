using Application.Users.DTOs;
using MediatR;

namespace Application.Users.Queries.ListUsers;

public sealed record ListUsersQuery(
    int Page = ListUsersQuery.DefaultPage,
    int PageSize = ListUsersQuery.DefaultPageSize,
    string? SortBy = ListUsersQuery.DefaultSortBy,
    string? SortDirection = ListUsersQuery.DefaultSortDirection) : IRequest<UserPageDto>
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const string DefaultSortBy = "createdAt";
    public const string DefaultSortDirection = "desc";

    internal static readonly IReadOnlyDictionary<string, UserSortField> SortFields =
        new Dictionary<string, UserSortField>(StringComparer.Ordinal)
        {
            ["fullName"] = UserSortField.FullName,
            ["email"] = UserSortField.Email,
            ["cpf"] = UserSortField.Cpf,
            ["createdAt"] = UserSortField.CreatedAt,
            ["status"] = UserSortField.Status
        };

    internal static readonly IReadOnlyDictionary<string, SortDirection> SortDirections =
        new Dictionary<string, SortDirection>(StringComparer.Ordinal)
        {
            ["asc"] = ListUsers.SortDirection.Asc,
            ["desc"] = ListUsers.SortDirection.Desc
        };
}
