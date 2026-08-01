using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// The one piece of Wolverine configuration a state-stored host must apply itself.
/// </summary>
/// <remarks>
/// Everything else is applied automatically by <c>BuildingBlocksWolverineExtension</c> when the host calls
/// <c>UseWolverine</c> (ADR-0027). The EF Core outbox is the documented exception: as of Wolverine 3.0 an
/// <see cref="IWolverineExtension"/> resolved from the container may no longer modify the service collection, and both
/// halves of the EF outbox — the PostgreSQL-backed message store and the transactional middleware — do exactly that.
/// Applying them from the extension therefore fails at host start with "the service collection cannot be modified
/// because it is read-only"; the only place left where the collection is still mutable is the host's own
/// <c>UseWolverine</c> callback.
/// </remarks>
public static class WolverineHostExtensions
{
    /// <summary>
    /// Applies Wolverine's PostgreSQL-backed message store and EF Core transactional middleware.
    /// </summary>
    /// <remarks>
    /// Call this from the <c>UseWolverine</c> callback of a host that selected <c>UseEfCorePersistence</c>, passing the
    /// same write-database connection string. The store lives in that database, so outbox rows and aggregate state
    /// share one database and one transaction (ADR-0021/0022). Event-sourced hosts must not call it — Marten supplies
    /// their message store through <c>IntegrateWithWolverine</c>.
    /// </remarks>
    /// <param name="options">The Wolverine options being configured.</param>
    /// <param name="writeConnectionString">The connection string of the context's write database.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="writeConnectionString"/> is <see langword="null"/>.</exception>
    public static WolverineOptions UseBuildingBlocksEfCorePersistence(
        this WolverineOptions options,
        string writeConnectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(writeConnectionString);

        options.PersistMessagesWithPostgresql(writeConnectionString);
        options.UseEntityFrameworkCoreTransactions();

        return options;
    }
}
