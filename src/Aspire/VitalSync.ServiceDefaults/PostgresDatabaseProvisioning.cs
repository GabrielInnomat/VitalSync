using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace VitalSync.ServiceDefaults;

public static class PostgresDatabaseProvisioning
{
    private const string DuplicateDatabase = "42P04";
    private const string MaintenanceDatabase = "postgres";

    private static readonly TimeSpan DefaultServerWait = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private static readonly Action<ILogger, string, Exception?> DatabaseCreatedAction =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, "DatabaseCreated"),
            "Created the database '{Database}'.");

    private static readonly Action<ILogger, string, Exception?> DatabaseExistsAction =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, "DatabaseExists"),
            "The database '{Database}' already exists.");

    private static readonly Action<ILogger, string, Exception?> ServerNotReadyAction =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3, "ServerNotReady"),
            "The PostgreSQL server for '{Database}' is not reachable yet; retrying.");

    public static async Task EnsureDatabasesExistAsync(
        this IHost host,
        IReadOnlyCollection<string> connectionNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(connectionNames);

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(PostgresDatabaseProvisioning));

        foreach (var connectionName in connectionNames)
        {
            var connectionString = configuration.GetConnectionString(connectionName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"No connection string named '{connectionName}' is configured, so its database cannot be created. " +
                    "Does the AppHost reference a database of that name?");
            }

            await EnsureDatabaseExistsAsync(connectionString, logger, DefaultServerWait, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<bool> EnsureDatabaseExistsAsync(
        string connectionString,
        ILogger logger,
        TimeSpan serverWait,
        CancellationToken cancellationToken)
    {
        var target = new NpgsqlConnectionStringBuilder(connectionString);
        var database = target.Database;
        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("The connection string names no database to create.");
        }

        var maintenance = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = MaintenanceDatabase,
            Pooling = false,
        };

        var dataSource = NpgsqlDataSource.Create(maintenance.ConnectionString);
        await using (dataSource.ConfigureAwait(false))
        {
            var connection = await OpenWhenReachableAsync(dataSource, database, logger, serverWait, cancellationToken)
                .ConfigureAwait(false);
            await using (connection.ConfigureAwait(false))
            {
                var created = !await DatabaseExistsAsync(connection, database, cancellationToken).ConfigureAwait(false)
                    && await CreateDatabaseAsync(connection, database, cancellationToken).ConfigureAwait(false);

                if (created)
                {
                    DatabaseCreatedAction(logger, database, null);
                }
                else
                {
                    DatabaseExistsAction(logger, database, null);
                }

                return created;
            }
        }
    }

    private static async Task<bool> DatabaseExistsAsync(NpgsqlConnection connection, string database, CancellationToken cancellationToken)
    {
        var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = $1", connection);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.Add(new NpgsqlParameter { Value = database });
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
        }
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "CREATE DATABASE takes no parameters; the name is quoted as an identifier.")]
    private static async Task<bool> CreateDatabaseAsync(NpgsqlConnection connection, string database, CancellationToken cancellationToken)
    {
        var command = new NpgsqlCommand($"CREATE DATABASE {QuoteIdentifier(database)}", connection);
        await using (command.ConfigureAwait(false))
        {
            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (PostgresException exception) when (exception.SqlState == DuplicateDatabase)
            {
                return false;
            }
        }
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static async Task<NpgsqlConnection> OpenWhenReachableAsync(
        NpgsqlDataSource dataSource,
        string database,
        ILogger logger,
        TimeSpan serverWait,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + serverWait;

        while (true)
        {
            try
            {
                return await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (NpgsqlException exception) when (exception.IsTransient && DateTimeOffset.UtcNow < deadline)
            {
                ServerNotReadyAction(logger, database, null);
                await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
