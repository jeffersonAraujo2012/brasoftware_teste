using Application.Users;

namespace Application.Tests.Users;

public sealed class ListUsersValidatorTests
{
    [Fact]
    public void DefaultQueryIsValid()
    {
        var result = new ListUsersValidator().Validate(new ListUsersQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void PageSizeBoundariesAreValid(int pageSize)
    {
        var result = new ListUsersValidator().Validate(new ListUsersQuery(PageSize: pageSize));

        Assert.True(result.IsValid);
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
        var result = new ListUsersValidator().Validate(
            new ListUsersQuery(SortBy: field, SortDirection: direction));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 20, "createdAt", "desc", "Page")]
    [InlineData(1, 0, "createdAt", "desc", "PageSize")]
    [InlineData(1, 101, "createdAt", "desc", "PageSize")]
    [InlineData(1, 20, "id", "asc", "SortBy")]
    [InlineData(1, 20, "createdAt", "up", "SortDirection")]
    [InlineData(1, 20, null, "desc", "SortBy")]
    public void InvalidParametersReportTheField(int page, int size, string? field, string direction, string expected)
    {
        var result = new ListUsersValidator().Validate(new ListUsersQuery(page, size, field, direction));

        Assert.Contains(result.Errors, failure => failure.PropertyName == expected);
    }
}
