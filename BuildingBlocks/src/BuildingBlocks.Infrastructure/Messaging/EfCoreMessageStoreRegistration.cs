using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Postgresql;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Registers Wolverine's PostgreSQL-backed durable message store on the host's service collection at composition time.
/// </summary>
/// <remarks>
/// Wolverine applies container-registered <see cref="IWolverineExtension"/>s only after the service provider is
/// built, so any <c>IServiceCollection</c> registrations they make are silently ineffective — which is exactly how
/// <see cref="BuildingBlocksWolverineExtension"/> runs. The EF Core outbox, however, refuses to commit without a
/// database-backed <c>IMessageStore</c> ("not using Database backed message persistence"), and Wolverine only ships
/// that registration inside <c>PersistMessagesWithPostgresql</c>, which needs the not-yet-existing
/// <c>WolverineOptions</c>. This helper bridges the gap from <c>UseEfCorePersistence</c>, while the collection is
/// still mutable: it points a throwaway <see cref="WolverineOptions"/> at the real service collection and lets
/// Wolverine's own <c>PostgresqlBackedPersistence.Configure</c> perform its service registrations (the message
/// store, its database settings, migrator, and database discovery). The codegen and policy side of the same
/// persistence is applied later against the real options by
/// <see cref="WolverineOptionsExtensions.ApplyBuildingBlockEfCoreOutbox"/>, where options mutations are effective.
/// The Wolverine internals touched here are pinned by the end-to-end outbox flush test, so a Wolverine upgrade that
/// moves them fails the build loudly instead of corrupting the outbox silently.
/// </remarks>
internal static class EfCoreMessageStoreRegistration
{
    private const string PersistenceTypeName = "Wolverine.Postgresql.PostgresqlBackedPersistence";

    /// <summary>
    /// Registers the PostgreSQL-backed message store services for the given write database.
    /// </summary>
    /// <remarks>
    /// Call once from <c>UseEfCorePersistence</c>; the store lives in the context's own write database so outbox
    /// rows and aggregate state share one database and one transaction (ADR-0021/0022).
    /// </remarks>
    /// <param name="services">The host's service collection, still mutable at composition time.</param>
    /// <param name="connectionString">The connection string of the context's write database.</param>
    /// <exception cref="InvalidOperationException">Thrown when the Wolverine internals this registration relies on have changed shape (for example after a Wolverine upgrade).</exception>
    public static void Register(IServiceCollection services, string connectionString)
    {
        var wolverinePostgresqlAssembly = typeof(PostgresqlConfigurationExtensions).Assembly;
        var persistenceType = wolverinePostgresqlAssembly.GetType(PersistenceTypeName)
            ?? throw MissingInternals($"type '{PersistenceTypeName}' was not found");

        var servicesProperty = typeof(WolverineOptions).GetProperty(nameof(WolverineOptions.Services))
            ?? throw MissingInternals($"property '{nameof(WolverineOptions)}.{nameof(WolverineOptions.Services)}' was not found");
        var connectionStringProperty = persistenceType.GetProperty("ConnectionString", BindingFlags.Public | BindingFlags.Instance)
            ?? throw MissingInternals($"property '{PersistenceTypeName}.ConnectionString' was not found");

        // A throwaway options object whose Services point at the real collection: Wolverine's own Configure then
        // performs the service registrations exactly as PersistMessagesWithPostgresql would, but at a moment when
        // they still take effect. Codegen/policy mutations land on the throwaway options and are re-applied against
        // the real options by ApplyBuildingBlockEfCoreOutbox when Wolverine bootstraps.
        var throwawayOptions = new WolverineOptions();
        servicesProperty.SetValue(throwawayOptions, services);

        var persistence = Activator.CreateInstance(persistenceType, throwawayOptions.Durability, throwawayOptions)
            ?? throw MissingInternals($"type '{PersistenceTypeName}' could not be constructed");
        connectionStringProperty.SetValue(persistence, connectionString);

        ((IWolverineExtension)persistence).Configure(throwawayOptions);
    }

    private static InvalidOperationException MissingInternals(string detail)
        => new(
            $"Registering Wolverine's PostgreSQL-backed message store failed: {detail}. " +
            "The Wolverine internals used by UseEfCorePersistence have changed shape — align " +
            $"{nameof(EfCoreMessageStoreRegistration)} with the installed Wolverine version.");
}
