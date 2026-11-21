using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace VitalSync.ServiceDefaults.Tests;

public sealed class PostgresServer : IAsyncLifetime
{
    private const string RequireContainersVariable = "THESSERA_REQUIRE_CONTAINERS";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18.3").Build();

    public string? SkipReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            await container.StartAsync();
        }
        catch (DockerUnavailableException exception) when (Environment.GetEnvironmentVariable(RequireContainersVariable) != "1")
        {
            SkipReason = $"PostgreSQL could not be started in a container: {exception.Message}";
        }
    }

    public async ValueTask DisposeAsync() => await container.DisposeAsync();

    public string ConnectionStringFor(string database) =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = database }.ConnectionString;

    public async Task<bool> DatabaseExistsAsync(string database)
    {
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = $1", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = database });
        return await command.ExecuteScalarAsync() is not null;
    }
}
