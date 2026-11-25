using FluentValidation;

namespace Application.Users.Queries.ListUsers;

public sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, ListUsersQuery.MaxPageSize);

        RuleFor(x => x.SortBy)
            .NotEmpty()
            .Must(sortBy => ListUsersQuery.SortFields.ContainsKey(sortBy!))
            .WithMessage("Sort field must be fullName, email, cpf, createdAt, or status.");

        RuleFor(x => x.SortDirection)
            .NotEmpty()
            .Must(direction => ListUsersQuery.SortDirections.ContainsKey(direction!))
            .WithMessage("Sort direction must be asc or desc.");
    }
}
