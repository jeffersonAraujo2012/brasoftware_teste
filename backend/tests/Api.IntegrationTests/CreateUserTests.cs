using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.IntegrationTests.Fixtures;
using Domain.ValueObjects;
using Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class CreateUserTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateReturns201LocationAndNormalizedUser()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = CreateTimedClient(factory);

        using var response = await client.PostAsJsonAsync("/api/users",
            new { fullName = " Sample Person ", email = " PERSON@EXAMPLE.TEST ", cpf = "012.345.678-90" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = user.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"/api/users/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Sample Person", user.GetProperty("fullName").GetString());
        Assert.Equal("person@example.test", user.GetProperty("email").GetString());
        Assert.Equal("01234567890", user.GetProperty("cpf").GetString());
        Assert.Equal("active", user.GetProperty("status").GetString());
        Assert.Equal("2026-01-01T10:00:00Z", user.GetProperty("createdAt").GetString());
    }

    [Fact]
    public async Task CreateIgnoresServerManagedFields()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = CreateTimedClient(factory);
        var suppliedId = Guid.Parse("b4fd88cd-67aa-4ff7-9fa3-bf21cabc7cd7");

        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            id = suppliedId,
            fullName = "Sample Person",
            email = "person@example.test",
            cpf = "01234567890",
            createdAt = "2000-01-01T00:00:00Z",
            status = "inactive"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(suppliedId, user.GetProperty("id").GetGuid());
        Assert.Equal("2026-01-01T10:00:00Z", user.GetProperty("createdAt").GetString());
        Assert.Equal("inactive", user.GetProperty("status").GetString());
    }

    [Fact]
    public async Task InvalidCreateReturnsFieldErrors()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/users",
            new { fullName = "", email = "person@example.test", cpf = "01234567891", status = (string?)null });

        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.TryGetProperty("fullName", out _));
        Assert.True(errors.TryGetProperty("cpf", out _));
        Assert.True(errors.TryGetProperty("status", out _));
    }

    [Theory]
    [InlineData("{", "body")]
    [InlineData("{\"status\":42}", "status")]
    [InlineData("{\"status\":\"ACTIVE\"}", "status")]
    [InlineData("{\"status\":\"pending\"}", "status")]
    public async Task MalformedJsonReturnsProblemDetails(string json, string field)
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/users", content);

        var errors = await ReadValidationErrorsAsync(response);
        Assert.True(errors.TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("PERSON@EXAMPLE.TEST", "12345678909", "email")]
    [InlineData("other@example.test", "012.345.678-90", "cpf")]
    public async Task CreateCollisionReturns409(string email, string cpf, string field)
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        using var created = await client.PostAsJsonAsync("/api/users",
            new { fullName = "Sample Person", email = "person@example.test", cpf = "01234567890" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var response = await client.PostAsJsonAsync("/api/users",
            new { fullName = "Other Person", email, cpf });

        await AssertConflictAsync(response, field);
    }

    [Fact]
    public async Task ConcurrentCreatesWithSameCpfAcceptExactlyOne()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        const string cpf = "01234567890";
        var storedCpf = Cpf.Create(cpf);

        var requests = await Task.WhenAll(
            client.PostAsJsonAsync("/api/users", new { fullName = "First", email = "first@example.test", cpf }),
            client.PostAsJsonAsync("/api/users", new { fullName = "Second", email = "second@example.test", cpf }));

        using var first = requests[0];
        using var second = requests[1];
        Assert.Single(requests, response => response.StatusCode == HttpStatusCode.Created);
        await AssertConflictAsync(Assert.Single(requests, response => response.StatusCode == HttpStatusCode.Conflict), "cpf");

        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        Assert.Equal(1, await database.Users.CountAsync(user => user.Cpf == storedCpf));
    }

    private static HttpClient CreateTimedClient(ApiFactory factory) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(CreatedAt)))).CreateClient();

    private static async Task<JsonElement> ReadValidationErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        return problem.GetProperty("errors");
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(field, problem.GetProperty("field").GetString());
    }
}
