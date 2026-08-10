using System.Globalization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace BuildingBlocks.Infrastructure.Diagnostics;

internal sealed class DeadLetterHealthCheck(DeadLetterInspector inspector) : IHealthCheck
{
    public const string Name = "building-blocks-dead-letters";

    public const string Tag = "dead-letters";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var reading = await inspector.CountAsync(cancellationToken).ConfigureAwait(false);

        if (reading.TableMissing)
        {
            return HealthCheckResult.Degraded(
                $"The table '{DeadLetterInspector.TableName}' does not exist, so a message this host gave up on "
                + "would not be visible here. Wolverine creates it with the rest of its message store; a host that "
                + "does not provision infrastructure itself depends on its migration worker having run first.");
        }

        if (reading.Count == 0)
        {
            return HealthCheckResult.Healthy("No message has been dead-lettered.");
        }

        var count = reading.Count.ToString(CultureInfo.InvariantCulture);
        var qualifier = reading.Capped ? "At least " : string.Empty;

        return HealthCheckResult.Degraded(
            $"{qualifier}{count} message(s) were given up on and moved to '{DeadLetterInspector.TableName}'. "
            + "This host keeps serving requests, which is why this is degraded rather than unhealthy, but the work "
            + "in those messages did not happen: a dead-lettered projection envelope means the read model is "
            + "missing that change and will stay wrong until the projection is fixed and the read model rebuilt.",
            data: new Dictionary<string, object> { ["count"] = reading.Count, ["capped"] = reading.Capped });
    }
}

internal sealed record DeadLetterReading(long Count, bool Capped, bool TableMissing);

internal sealed class DeadLetterInspector(NpgsqlDataSource dataSource) : IDisposable
{
    public const string TableName = "wolverine_dead_letters";

    private const int Ceiling = 1000;

    private const string UndefinedTable = "42P01";

    private const string CountQuery = "select count(*) from (select 1 from wolverine_dead_letters limit 1000) as capped";

    public async Task<DeadLetterReading> CountAsync(CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (connection.ConfigureAwait(false))
        {
            return await ReadAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose() => dataSource.Dispose();

    private static async Task<DeadLetterReading> ReadAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = CountQuery;

            try
            {
                var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                var count = Convert.ToInt64(scalar, CultureInfo.InvariantCulture);

                return new DeadLetterReading(count, count >= Ceiling, TableMissing: false);
            }
            catch (PostgresException exception) when (exception.SqlState == UndefinedTable)
            {
                return new DeadLetterReading(0, Capped: false, TableMissing: true);
            }
        }
    }
}
