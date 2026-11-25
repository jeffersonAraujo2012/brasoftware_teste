using Application.Users.Queries.ListUsers;

namespace Application.Tests.Users.Queries.ListUsers;

public sealed class ListUsersQueryValidatorTests
{
    private readonly ListUsersQueryValidator _validator = new();

    [Fact]
    public void DefaultQueryIsValid()
    {
        Assert.True(_validator.Validate(new ListUsersQuery()).IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void PageSizeBoundariesAreValid(int pageSize)
    {
        Assert.True(_validator.Validate(new ListUsersQuery(PageSize: pageSize)).IsValid);
    }

    [Theory]
    [InlineData("fullName", "asc")]
    [InlineData("fullName", "desc")]
    [InlineData("email", "asc")]
    [InlineData("email", "desc")]
    [InlineData("cpf", "asc")]
    [InlineData("cpf", "desc")]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    [InlineData("status", "asc")]
    [InlineData("status", "desc")]
    public void AcceptsSupportedSort(string field, string direction)
    {
        Assert.True(_validator.Validate(new ListUsersQuery(SortBy: field, SortDirection: direction)).IsValid);
    }

    [Theory]
    [InlineData(0, 20, "createdAt", "desc", "Page")]
    [InlineData(1, 0, "createdAt", "desc", "PageSize")]
    [InlineData(1, 101, "createdAt", "desc", "PageSize")]
    [InlineData(1, 20, "id", "asc", "SortBy")]
    [InlineData(1, 20, null, "desc", "SortBy")]
    [InlineData(1, 20, "createdAt", "up", "SortDirection")]
    [InlineData(1, 20, "createdAt", null, "SortDirection")]
    public void InvalidParametersReportTheField(int page, int size, string? field, string? direction, string expected)
    {
        var result = _validator.Validate(new ListUsersQuery(page, size, field, direction));

        Assert.Contains(result.Errors, failure => failure.PropertyName == expected);
    }
}
