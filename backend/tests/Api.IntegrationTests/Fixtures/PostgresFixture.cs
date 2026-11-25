using Npgsql;
using Testcontainers.PostgreSql;

namespace Api.IntegrationTests.Fixtures;

[CollectionDefinition("PostgreSQL")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-bookworm").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task<string> CreateDatabaseAsync()
    {
        var database = $"tests_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE {database}", connection);
        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = database
        }.ConnectionString;
    }

    public async Task<long> TerminateDatabaseConnectionsAsync(string targetConnectionString)
    {
        var database = new NpgsqlConnectionStringBuilder(targetConnectionString).Database!;
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            WITH connections AS MATERIALIZED (
                SELECT pid FROM pg_stat_activity WHERE datname = @database
            )
            SELECT count(*) FROM connections WHERE pg_terminate_backend(pid, 5000)
            """, connection);
        command.Parameters.AddWithValue("database", database);

        return (long)(await command.ExecuteScalarAsync())!;
    }
}
