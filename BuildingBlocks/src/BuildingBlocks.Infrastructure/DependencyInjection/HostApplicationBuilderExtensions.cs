using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// The host-builder entry point of <c>BuildingBlocks.Infrastructure</c>: one call that registers the platform
/// services and configures Wolverine for them.
/// </summary>
/// <remarks>
/// This is the only way to wire a state-stored context: because it owns the <c>UseWolverine</c> call, it applies the
/// EF Core outbox from the connection string the host already declared through <c>UseEfCorePersistence</c> — the host
/// names its write database exactly once, which is what restores ADR-0027's promise that the host configures nothing.
/// The <see cref="IServiceCollection"/> overload remains for hosts that build no host builder and for tests that need
/// registrations only; it can register handlers, Marten, and messaging, but a host using it must issue
/// <c>UseWolverine</c> itself and cannot obtain the EF Core outbox — the startup validator says so at host start.
/// </remarks>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the Building Blocks platform services and, when the selection needs it, configures Wolverine.
    /// </summary>
    /// <remarks>
    /// Calls <c>UseWolverine</c> on the builder when the capability selection requires a Wolverine runtime — a
    /// persistence style, the messaging transport, or an integration-event subscription — or when
    /// <paramref name="configureWolverine"/> is supplied. For a state-stored context it additionally applies
    /// Wolverine's PostgreSQL-backed message store and EF Core transactional middleware against the very write
    /// database that <c>UseEfCorePersistence</c> selected, so outbox rows and aggregate state cannot end up in
    /// different databases (ADR-0022). The host must not call <c>UseWolverine</c> itself afterwards — Wolverine
    /// permits it only once; host-specific transport settings belong in <paramref name="configureWolverine"/>.
    /// </remarks>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host application builder to register into.</param>
    /// <param name="configure">The callback that selects handlers, persistence style, and messaging via <see cref="BuildingBlocksOptions"/>.</param>
    /// <param name="configureWolverine">An optional callback for host-specific Wolverine configuration, applied after the Building Block defaults.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static TBuilder AddBuildingBlocks<TBuilder>(
        this TBuilder builder,
        Action<BuildingBlocksOptions> configure,
        Action<WolverineOptions>? configureWolverine = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var wiring = ServiceCollectionExtensions.AddBuildingBlocksCore(builder.Services, configure);

        if (!wiring.RequiresWolverine && configureWolverine is null)
        {
            return builder;
        }

        builder.UseWolverine(options =>
        {
            // The EF Core outbox is the one thing a container-registered IWolverineExtension may not apply:
            // as of Wolverine 3.0 such an extension may no longer modify the service collection, and both
            // halves below do exactly that ("the service collection cannot be modified because it is
            // read-only"). A UseWolverine callback still may, which is why they live here. The connection
            // string comes from the selection the host already made, never from a second parameter.
            if (wiring.EfCoreMessageStoreConnectionString is { } writeConnectionString)
            {
                options.PersistMessagesWithPostgresql(writeConnectionString);
                options.UseEntityFrameworkCoreTransactions();
            }

            configureWolverine?.Invoke(options);
        });

        return builder;
    }
}
