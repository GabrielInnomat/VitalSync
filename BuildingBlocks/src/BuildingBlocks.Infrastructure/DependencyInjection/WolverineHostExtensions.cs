using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// The piece of Wolverine configuration that cannot come from a container-registered extension.
/// </summary>
/// <remarks>
/// Everything else is applied automatically by <c>BuildingBlocksWolverineExtension</c> when Wolverine bootstraps
/// (ADR-0027). The EF Core outbox is the exception: as of Wolverine 3.0 an <see cref="IWolverineExtension"/> resolved
/// from the container may no longer modify the service collection, and both halves of the EF outbox — the
/// PostgreSQL-backed message store and the transactional middleware — do exactly that. Applying them from the
/// extension fails at host start with "the service collection cannot be modified because it is read-only", so they
/// must be applied from a <c>UseWolverine</c> callback instead. Hosts using
/// <see cref="HostApplicationBuilderExtensions.AddBuildingBlocks"/> need not call this at all — that overload owns the
/// <c>UseWolverine</c> call and applies it from the connection string the host already selected. It stays public for
/// hosts that wire Wolverine themselves on top of the <see cref="ServiceCollectionExtensions.AddBuildingBlocks"/>
/// overload; those, and only those, pass the write connection string a second time.
/// </remarks>
public static class WolverineHostExtensions
{
    /// <summary>
    /// Applies Wolverine's PostgreSQL-backed message store and EF Core transactional middleware.
    /// </summary>
    /// <remarks>
    /// Call this from the <c>UseWolverine</c> callback of a host that selected <c>UseEfCorePersistence</c> and wires
    /// Wolverine itself, passing the same write-database connection string. The store lives in that database, so
    /// outbox rows and aggregate state share one database and one transaction (ADR-0021/0022) — passing a different
    /// database here is the one mistake this path still allows, which is why
    /// <see cref="HostApplicationBuilderExtensions.AddBuildingBlocks"/> exists and calls this method for the host.
    /// Event-sourced hosts must not call it — Marten supplies their message store through <c>IntegrateWithWolverine</c>.
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
