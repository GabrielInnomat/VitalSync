using BuildingBlocks.Application.Results;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Postgresql;

namespace BuildingBlocks.Infrastructure.Persistence.StateStored;

internal sealed class PostgresDatabaseDriver : IEfCoreDatabaseDriver
{
    public static PostgresDatabaseDriver Instance { get; } = new();

    public IReadOnlyList<IPersistenceFaultTranslator> FaultTranslators { get; } = [new PostgresFaultTranslator()];

    public void ConfigureContext(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseNpgsql(connectionString);

    public void PersistMessages(WolverineOptions options, string connectionString) =>
        options.PersistMessagesWithPostgresql(connectionString);

    public bool IsTransientFault(Exception exception) => PostgresTransientFaults.IsTransient(exception);
}
