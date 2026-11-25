using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Fixtures;
using Domain.Users;
using Infra.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class ListUsersTests(PostgresFixture postgres)
{
    private static readonly Seed[] Seeds =
    [
        new(4, "Morgan Lee", "m@example.test", "11144477735", 2, UserStatus.Inactive),
        new(3, "Alex Brown", "a@example.test", "52998224725", 1, UserStatus.Active),
        new(2, "Alex Brown", "b@example.test", "01234567890", 3, UserStatus.Active),
        new(1, "Taylor Young", "z@example.test", "12345678909", 2, UserStatus.Inactive)
    ];

    [Fact]
    public async Task DefaultsReturnPageOneWithNewestFirst()
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient();

        var page = await ReadPageAsync(client);

        AssertMetadata(page, pageNumber: 1, pageSize: 20, totalItems: 4, totalPages: 1);
        Assert.Equal(ExpectedIds("BADC"), ListedIds(page));
    }

    [Fact]
    public async Task ChangingPageSizeReturnsOnlyTheRequestedPage()
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient();

        var page = await ReadPageAsync(client, "?page=2&pageSize=3");

        AssertMetadata(page, pageNumber: 2, pageSize: 3, totalItems: 4, totalPages: 2);
        Assert.Equal(ExpectedIds("C"), ListedIds(page));
    }

    [Fact]
    public async Task EmptyListReturnsZeroPages()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();

        var page = await ReadPageAsync(client);

        AssertMetadata(page, pageNumber: 1, pageSize: 20, totalItems: 0, totalPages: 0);
        Assert.Empty(ListedIds(page));
    }

    [Fact]
    public async Task FarPageReturnsEmptyItemsWithTotals()
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient();

        var page = await ReadPageAsync(client, "?page=2147483647&pageSize=100");

        AssertMetadata(page, pageNumber: int.MaxValue, pageSize: 100, totalItems: 4, totalPages: 1);
        Assert.Empty(ListedIds(page));
    }

    [Theory]
    [InlineData("fullName", "asc", "BCDA")]
    [InlineData("fullName", "desc", "ADBC")]
    [InlineData("email", "asc", "CBDA")]
    [InlineData("email", "desc", "ADBC")]
    [InlineData("cpf", "asc", "BDAC")]
    [InlineData("cpf", "desc", "CADB")]
    [InlineData("createdAt", "asc", "CADB")]
    [InlineData("createdAt", "desc", "BADC")]
    [InlineData("status", "asc", "BCAD")]
    [InlineData("status", "desc", "ADBC")]
    public async Task SortsByRequestedFieldAndDirection(string field, string direction, string expected)
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient();

        var page = await ReadPageAsync(client, $"?sortBy={field}&sortDirection={direction}");

        Assert.Equal(ExpectedIds(expected), ListedIds(page));
    }

    [Theory]
    [InlineData("asc", 1)]
    [InlineData("desc", 3)]
    public async Task NameTiesKeepIdAscendingAcrossPages(string direction, int firstPage)
    {
        await using var factory = await CreateSeededFactoryAsync();
        using var client = factory.CreateClient();
        var query = $"?pageSize=1&sortBy=fullName&sortDirection={direction}";

        var first = await ReadPageAsync(client, $"{query}&page={firstPage}");
        var second = await ReadPageAsync(client, $"{query}&page={firstPage + 1}");

        Assert.Equal(UserId(2), Assert.Single(ListedIds(first)));
        Assert.Equal(UserId(3), Assert.Single(ListedIds(second)));
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("page=abc", "page")]
    [InlineData("page=", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("sortBy=id", "sortBy")]
    [InlineData("sortBy=", "sortBy")]
    [InlineData("sortDirection=up", "sortDirection")]
    public async Task InvalidListParametersReturn400(string query, string field)
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/users?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task OpenApiDocumentsListQueryParameters()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var users = document.GetProperty("paths").GetProperty("/api/users");
        Assert.True(users.TryGetProperty("get", out var operation));
        var parameters = operation.GetProperty("parameters").EnumerateArray().ToDictionary(
            parameter => parameter.GetProperty("name").GetString()!);

        Assert.Equal(new[] { "page", "pageSize", "sortBy", "sortDirection" }, parameters.Keys.Order());
        Assert.Contains("1 to 100", parameters["pageSize"].GetProperty("description").GetString());
    }

    private async Task<ApiFactory> CreateSeededFactoryAsync()
    {
        var factory = await ApiFactory.CreateAsync(postgres);
        try
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
            database.Users.AddRange(Seeds.Select(seed => User.Create(
                UserId(seed.Id), seed.FullName, seed.Email, seed.Cpf,
                new DateTime(2026, 1, seed.Day, 10, 0, 0, DateTimeKind.Utc), seed.Status)));
            await database.SaveChangesAsync();
            return factory;
        }
        catch
        {
            await factory.DisposeAsync();
            throw;
        }
    }

    private static async Task<JsonElement> ReadPageAsync(HttpClient client, string query = "")
    {
        using var response = await client.GetAsync($"/api/users{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static void AssertMetadata(JsonElement page, int pageNumber, int pageSize, int totalItems, int totalPages)
    {
        Assert.Equal(pageNumber, page.GetProperty("page").GetInt32());
        Assert.Equal(pageSize, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(totalItems, page.GetProperty("totalItems").GetInt32());
        Assert.Equal(totalPages, page.GetProperty("totalPages").GetInt32());
    }

    private static IEnumerable<Guid> ListedIds(JsonElement page)
        => page.GetProperty("items").EnumerateArray().Select(user => user.GetProperty("id").GetGuid());

    private static IEnumerable<Guid> ExpectedIds(string sequence) => sequence.Select(letter => UserId(letter - 'A' + 1));

    private static Guid UserId(int id) => Guid.Parse($"00000000-0000-0000-0000-{id:D12}");

    private sealed record Seed(int Id, string FullName, string Email, string Cpf, int Day, UserStatus Status);
}
