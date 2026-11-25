using Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Application.Users;

public sealed record ListUsersQuery(
    int Page = ListUsersQuery.DefaultPage,
    int PageSize = ListUsersQuery.DefaultPageSize,
    string? SortBy = ListUsersQuery.DefaultSortBy,
    string? SortDirection = ListUsersQuery.DefaultSortDirection) : IRequest<ListUsersResult>
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
}

public sealed record ListUsersResult(IReadOnlyList<UserDetails> Items, int Page, int PageSize, int TotalItems,
    int TotalPages);

public enum UserSortField
{
    FullName,
    Email,
    Cpf,
    CreatedAt,
    Status
}

public enum SortDirection
{
    Asc,
    Desc
}

public sealed class ListUsersValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, ListUsersQuery.MaxPageSize);
        RuleFor(query => query.SortBy)
            .Must(field => field is not null && ListUsersQuery.SortFields.ContainsKey(field))
            .WithMessage("Sort field must be fullName, email, cpf, createdAt, or status.");
        RuleFor(query => query.SortDirection)
            .Must(direction => direction is "asc" or "desc")
            .WithMessage("Sort direction must be asc or desc.");
    }
}

internal sealed class ListUsersHandler(IUserRepository repository) : IRequestHandler<ListUsersQuery, ListUsersResult>
{
    public async Task<ListUsersResult> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var sortField = ListUsersQuery.SortFields[request.SortBy!];
        var sortDirection = request.SortDirection == "asc" ? SortDirection.Asc : SortDirection.Desc;
        var (items, totalItems) = await repository.ListAsync(
            request.Page, request.PageSize, sortField, sortDirection, cancellationToken);
        var totalPages = totalItems == 0 ? 0 : 1 + (totalItems - 1) / request.PageSize;

        return new ListUsersResult(items.Select(UserDetails.From).ToArray(),
            request.Page, request.PageSize, totalItems, totalPages);
    }
}
