using Infra.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests.Fixtures;

internal sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public static async Task<ApiFactory> CreateAsync(PostgresFixture postgres)
    {
        var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
        try
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
            await database.Database.MigrateAsync();
            return factory;
        }
        catch
        {
            await factory.DisposeAsync();
            throw;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionString }));
    }
}
