using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Fixtures;
using Application.Abstractions;
using Application.Users;
using Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class UserLifecycleTests(PostgresFixture postgres)
{
    private const string MissingId = "a62c5805-cab8-4723-84e4-ee1ab50b36c9";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    [Fact]
    public async Task GetUpdateDeleteUserLifecycle()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        var (location, original) = await CreateUserAsync(client);

        using var get = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var found = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(original.GetProperty("id").GetGuid(), found.GetProperty("id").GetGuid());

        using var put = await client.PutAsJsonAsync(location,
            new { fullName = " Updated Person ", email = " NEW@EXAMPLE.TEST ", cpf = "529.982.247-25", status = "inactive" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(original.GetProperty("id").GetGuid(), updated.GetProperty("id").GetGuid());
        Assert.Equal(original.GetProperty("createdAt").GetDateTime(), updated.GetProperty("createdAt").GetDateTime());
        Assert.Equal("Updated Person", updated.GetProperty("fullName").GetString());
        Assert.Equal("new@example.test", updated.GetProperty("email").GetString());
        Assert.Equal("52998224725", updated.GetProperty("cpf").GetString());

        using var reloaded = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, reloaded.StatusCode);
        Assert.Equal("inactive", (await reloaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        using var deleted = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsByteArrayAsync());
        using var missing = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task CreatedTimestampMatchesTheValueReadFromPostgres()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero).AddTicks(1_234_569));
        await using var timedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(
            services => services.AddSingleton<TimeProvider>(clock)));
        using var client = timedFactory.CreateClient();

        var (location, created) = await CreateUserAsync(client);
        using var response = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var persisted = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("2026-01-01T10:00:00.123456Z", persisted.GetProperty("createdAt").GetString());
        Assert.Equal(persisted.GetProperty("createdAt").GetString(), created.GetProperty("createdAt").GetString());
    }

    [Theory]
    [InlineData("GET", "not-a-uuid")]
    [InlineData("PUT", "not-a-uuid")]
    [InlineData("DELETE", "not-a-uuid")]
    [InlineData("GET", EmptyId)]
    [InlineData("PUT", EmptyId)]
    [InlineData("DELETE", EmptyId)]
    public async Task InvalidIdReturns400(string method, string id)
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();

        using var request = CreateRequest(method, id);
        using var response = await client.SendAsync(request);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("id", out _));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingUserReturns404(string method)
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();

        using var request = CreateRequest(method, MissingId);
        using var response = await client.SendAsync(request);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains("user", problem.GetProperty("title").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateRequiresStatus()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        var (location, _) = await CreateUserAsync(client);

        using var response = await client.PutAsJsonAsync(location,
            new { fullName = "Sample Person", email = "person@example.test", cpf = "01234567890" });

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("status", out _));
    }

    [Theory]
    [InlineData("OTHER@EXAMPLE.TEST", "01234567890", "email")]
    [InlineData("person@example.test", "529.982.247-25", "cpf")]
    public async Task UpdateCollisionPreservesStoredUser(string email, string cpf, string field)
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        var (location, _) = await CreateUserAsync(client);
        await CreateUserAsync(client, "other@example.test", "52998224725");

        using var same = await client.PutAsJsonAsync(location,
            new { fullName = "Kept Profile", email = "PERSON@EXAMPLE.TEST", cpf = "012.345.678-90", status = "inactive" });
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);

        using var response = await client.PutAsJsonAsync(location,
            new { fullName = "Rejected Profile", email, cpf, status = "active" });

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal(field, problem.GetProperty("field").GetString());

        using var reloaded = await client.GetAsync(location);
        var user = await reloaded.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Kept Profile", user.GetProperty("fullName").GetString());
        Assert.Equal("person@example.test", user.GetProperty("email").GetString());
        Assert.Equal("01234567890", user.GetProperty("cpf").GetString());
        Assert.Equal("inactive", user.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UnexpectedErrorReturnsGenericProblem()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        await using var failingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.AddScoped<IUserRepository, FailingRepository>();
        }));
        using var client = failingFactory.CreateClient();

        using var response = await client.GetAsync($"/api/users/{MissingId}");

        await ReadProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.DoesNotContain("probe-internal-error", await response.Content.ReadAsStringAsync());
    }

    private static async Task<(Uri Location, JsonElement User)> CreateUserAsync(
        HttpClient client, string email = "person@example.test", string cpf = "01234567890")
    {
        using var response = await client.PostAsJsonAsync("/api/users", new { fullName = "Sample Person", email, cpf });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (response.Headers.Location, await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    private static HttpRequestMessage CreateRequest(string method, string id)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/users/{id}");
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(
                new { fullName = "Sample Person", email = "person@example.test", cpf = "01234567890", status = "active" });
        }

        return request;
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        return problem;
    }

    private sealed class FailingRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("probe-internal-error");

        public Task<(IReadOnlyList<User> Items, int TotalItems)> ListAsync(
            int page, int pageSize, UserSortField sortField, SortDirection sortDirection, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AddAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
