using System.Reflection;
using GaWeCodes.Persistence.EfCore.StateStored;
using GaWeCodes.Persistence.Npgsql;
using GaWeCodes.Testing;
using GaWeCodes.Wolverine.DependencyInjection.Wiring;

namespace GaWeCodes.Tests;

public sealed class PublicSurfaceTests
{
    private static readonly Assembly Core = typeof(ServiceCollectionExtensions).Assembly;

    private static readonly Assembly WolverineAdapter = typeof(WolverineRuntimeRegistration).Assembly;

    private static readonly Assembly NpgsqlFaults = typeof(PostgresTransientFaults).Assembly;

    private static readonly Assembly EfCore = typeof(IEfCoreDatabaseDriver).Assembly;

    private static readonly Assembly EfCorePostgres = typeof(EfCorePersistenceOptionsExtensions).Assembly;

    private static readonly Assembly Marten = typeof(MartenPersistenceOptionsExtensions).Assembly;

    private static readonly Assembly RabbitMq = typeof(RabbitMqMessagingExtensions).Assembly;

    private static readonly Assembly Testing = typeof(PersistedSchema).Assembly;

    private static readonly Assembly[] AllAssemblies =
        [Core, WolverineAdapter, NpgsqlFaults, EfCore, EfCorePostgres, Marten, RabbitMq, Testing];

    private static readonly string[] IntendedCoreApi =
    [
        "GaWeCodes.Core.DependencyInjection.BuildingBlocksOptions",
        "GaWeCodes.HostApplicationBuilderExtensions",
        "GaWeCodes.Core.DependencyInjection.InfrastructureProvisioning",
        "GaWeCodes.ServiceCollectionExtensions",
        "GaWeCodes.Core.Persistence.EntityKeyJsonOptions",
        "GaWeCodes.Core.Persistence.IPersistenceFaultTranslator",
        "GaWeCodes.Core.Startup.IStartupCheck",
        "GaWeCodes.Core.Startup.StartupPhase",
    ];

    private static readonly string[] IntendedCoreAdapterContract =
    [
        "GaWeCodes.Core.DependencyInjection.Extensibility.IRuntimeActivator",
        "GaWeCodes.Core.DependencyInjection.Extensibility.IWiringSnapshot",
        "GaWeCodes.Core.DependencyInjection.Extensibility.RuntimeActivation",
        "GaWeCodes.Core.DependencyInjection.Wiring.IntegrationEventSubscription",
        "GaWeCodes.Core.Messaging.DomainEvents.DomainEventMetadataFactory",
        "GaWeCodes.Core.Messaging.IntegrationEvents.TopicPatternMatcher",
        "GaWeCodes.Core.Messaging.IntegrationEvents.TopicResolver",
        "GaWeCodes.Core.Messaging.Transport.IMessageEmitter",
        "GaWeCodes.Core.Messaging.Transport.IMessagingTransportAdapter",
        "GaWeCodes.Core.Messaging.Transport.MessagingTransportRegistrationContext",
        "GaWeCodes.Core.Persistence.AggregateFactory",
        "GaWeCodes.Core.Persistence.AggregateStyle",
        "GaWeCodes.Core.Persistence.EntityKeyActivator",
        "GaWeCodes.Core.Persistence.IPersistenceAdapter",
        "GaWeCodes.Core.Persistence.PersistenceRegistrationContext",
        "GaWeCodes.Core.Startup.SynchronousStartupCheck",
    ];

    private static readonly string[] IntendedTestingApi =
    [
        "GaWeCodes.Testing.AggregateConventions",
        "GaWeCodes.Testing.PersistedSchema",
        "GaWeCodes.Testing.TestMetadata",
    ];

    private static readonly string[] IntendedWolverineApi =
    [
        "GaWeCodes.Wolverine.DependencyInjection.Wiring.WolverineRuntimeActivator",
        "GaWeCodes.Wolverine.DependencyInjection.Wiring.WolverineRuntimeRegistration",
        "GaWeCodes.WolverineRuntimeOptionsExtensions",
        "GaWeCodes.Wolverine.Diagnostics.DeadLetterHealthCheckRegistration",
        "GaWeCodes.Wolverine.Messaging.Transport.IWolverineMessagingTransport",
        "GaWeCodes.Wolverine.Persistence.IOutboxDurabilityConfigurator",
    ];

    // What a store author writes against. This was its own package until the store toolkit was
    // folded into the core; it stays a separate list because it is a distinct promise.
    private static readonly string[] IntendedCoreStoreAuthorApi =
    [
        "GaWeCodes.Core.Persistence.AggregateTracker`1",
        "GaWeCodes.Core.Persistence.DomainEventEnvelopeFactory",
        "GaWeCodes.Core.Persistence.EntityKeyFormatter",
        "GaWeCodes.Core.Persistence.ITrackedAggregate",
        "GaWeCodes.Core.Persistence.PersistenceFailureCodes",
        "GaWeCodes.Core.ReadModels.ReadModelRebuildWriter",
    ];

    private static readonly string[] IntendedNpgsqlApi =
    [
        "GaWeCodes.Persistence.Npgsql.PostgresFaultTranslator",
        "GaWeCodes.Persistence.Npgsql.PostgresTransientFaults",
    ];

    private static readonly string[] IntendedEfCoreApi =
    [
        "GaWeCodes.Persistence.EfCore.EntityKeyModelBuilderExtensions",
        "GaWeCodes.Persistence.EfCore.StateStored.EfCorePersistenceAdapter`1",
        "GaWeCodes.Persistence.EfCore.StateStored.IEfCoreDatabaseDriver",
        "GaWeCodes.Persistence.EfCore.ReadModels.StateStoredReadModelRebuildRunner`1",
    ];

    private static readonly string[] IntendedEfCorePostgresApi =
    [
        "GaWeCodes.EfCorePersistenceOptionsExtensions",
    ];

    private static readonly string[] IntendedMartenApi =
    [
        "GaWeCodes.MartenPersistenceOptionsExtensions",
        "GaWeCodes.Persistence.Marten.ReadModels.EventSourcedReadModelRebuildRunner",
    ];

    private static readonly string[] IntendedRabbitMqApi =
    [
        "GaWeCodes.RabbitMqMessagingExtensions",
    ];

    private static readonly string[] ExtensionPoints =
    [
        "GaWeCodes.Core.DependencyInjection.Extensibility.IRuntimeActivator",
        "GaWeCodes.Core.DependencyInjection.Extensibility.IWiringSnapshot",
        "GaWeCodes.Core.Messaging.Transport.IMessagingTransportAdapter",
        "GaWeCodes.Wolverine.Messaging.Transport.IWolverineMessagingTransport",
        "GaWeCodes.Core.Persistence.AggregateStyle",
        "GaWeCodes.Wolverine.Persistence.IOutboxDurabilityConfigurator",
        "GaWeCodes.Core.Persistence.IPersistenceAdapter",
        "GaWeCodes.Core.Persistence.IPersistenceFaultTranslator",
        "GaWeCodes.Core.Startup.IStartupCheck",
        "GaWeCodes.Core.Startup.StartupPhase",
        "GaWeCodes.Core.Startup.SynchronousStartupCheck",
    ];

    private static readonly string[] RequiredByWolverineCodeGeneration =
    [
        "GaWeCodes.Core.Messaging.DomainEvents.DomainEventEnvelope",
        "GaWeCodes.Wolverine.Messaging.DomainEvents.DomainEventEnvelopeHandler",
        "GaWeCodes.Core.Messaging.DomainEvents.DomainEventEnvelopeSerializer",
        "GaWeCodes.Core.Messaging.DomainEvents.DomainEventTypeRegistry",
        "GaWeCodes.Core.Messaging.DomainEvents.ProjectionEnvelope",
        "GaWeCodes.Wolverine.Messaging.DomainEvents.ProjectionEnvelopeHandler",
        "GaWeCodes.Core.Messaging.DomainEvents.ProjectionRunner",
        "GaWeCodes.Core.Messaging.IntegrationEvents.IIntegrationEventSinkFactory",
        "GaWeCodes.Core.Messaging.IntegrationEvents.IntegrationEventSourceContext",
        "GaWeCodes.Wolverine.Messaging.IntegrationEvents.OwnContextIntegrationEventFilter",
    ];

    private static readonly string[] CodeGenerationTypesInTheCore =
    [
        "GaWeCodes.Core.Messaging.DomainEvents.DomainEventEnvelope",
        "GaWeCodes.Core.Messaging.DomainEvents.DomainEventEnvelopeSerializer",
        "GaWeCodes.Core.Messaging.DomainEvents.DomainEventTypeRegistry",
        "GaWeCodes.Core.Messaging.DomainEvents.ProjectionEnvelope",
        "GaWeCodes.Core.Messaging.DomainEvents.ProjectionRunner",
        "GaWeCodes.Core.Messaging.IntegrationEvents.IIntegrationEventSinkFactory",
        "GaWeCodes.Core.Messaging.IntegrationEvents.IntegrationEventSourceContext",
    ];

    private static readonly string[] CodeGenerationTypesInTheWolverineAdapter =
    [
        "GaWeCodes.Wolverine.Messaging.DomainEvents.DomainEventEnvelopeHandler",
        "GaWeCodes.Wolverine.Messaging.DomainEvents.ProjectionEnvelopeHandler",
        "GaWeCodes.Wolverine.Messaging.IntegrationEvents.OwnContextIntegrationEventFilter",
    ];

    public static TheoryData<string, string[]> PinnedSurfaces =>
        new()
        {
            {
                "GaWeCodes.Core",
                [.. IntendedCoreApi
                    .Concat(IntendedCoreAdapterContract)
                    .Concat(IntendedCoreStoreAuthorApi)
                    .Concat(CodeGenerationTypesInTheCore)]
            },
            { "GaWeCodes.Testing", IntendedTestingApi },
            {
                "GaWeCodes.Wolverine",
                [.. IntendedWolverineApi.Concat(CodeGenerationTypesInTheWolverineAdapter)]
            },
            { "GaWeCodes.Persistence.Npgsql", IntendedNpgsqlApi },
            { "GaWeCodes.Persistence.EfCore", IntendedEfCoreApi },
            { "GaWeCodes.Persistence.EfCore.Postgres", IntendedEfCorePostgresApi },
            { "GaWeCodes.Persistence.Marten", IntendedMartenApi },
            { "GaWeCodes.Messaging.RabbitMq", IntendedRabbitMqApi },
        };

    [Theory]
    [MemberData(nameof(PinnedSurfaces))]
    public void ThePublicSurface_IsExactlyTheIntendedApiPlusWhatCodeGenerationForces(
        string assemblyName,
        string[] intended)
    {
        ArgumentNullException.ThrowIfNull(intended);

        var expected = intended.Order(StringComparer.Ordinal).ToArray();
        var actual = AssemblyNamed(assemblyName)
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NoInfrastructureImplementationIsPublic()
    {
        var intended = IntendedCoreApi
            .Concat(IntendedCoreAdapterContract)
            .Concat(IntendedWolverineApi)
            .Concat(IntendedCoreStoreAuthorApi)
            .Concat(IntendedNpgsqlApi)
            .Concat(IntendedEfCoreApi)
            .Concat(IntendedEfCorePostgresApi)
            .Concat(IntendedMartenApi)
            .Concat(IntendedRabbitMqApi)
            .ToArray();

        var leaked = AllAssemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.Namespace is not null
                && (type.Namespace.Contains(".Persistence", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Dispatching", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Events", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Time", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Wiring", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Registration", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Startup", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Validation", StringComparison.Ordinal)))
            .Select(type => type.FullName!)
            .Except(intended, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(leaked);
    }

    [Fact]
    public void TheCoreNamesNoSatelliteAssembly()
    {
        var satellites = new[] { WolverineAdapter, EfCore, Marten, RabbitMq }
            .Select(assembly => assembly.GetName().Name!)
            .ToArray();

        var referenced = Core.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Intersect(satellites, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(referenced);
    }

    [Fact]
    public void EveryExtensionPointIsAnAbstraction_NotAnImplementation()
    {
        foreach (var name in ExtensionPoints)
        {
            var type = TypeNamed(name);
            Assert.NotNull(type);
            Assert.True(
                type.IsInterface || type.IsEnum || type.IsAbstract,
                $"'{name}' is offered as an extension point, so it must be an abstraction consumers can implement.");
        }
    }

    [Fact]
    public void EveryTypeExemptedForCodeGeneration_IsActuallyReachableFromGeneratedCode()
    {
        foreach (var name in RequiredByWolverineCodeGeneration)
        {
            var type = TypeNamed(name);
            Assert.NotNull(type);
            Assert.True(type.IsPublic, $"'{name}' is listed as code-generation exempt but is not public.");
        }
    }

    [Fact]
    public void TheAssemblyExposesNoPublicField()
    {
        var fields = AllAssemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => !type.IsEnum)
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(field => !field.IsLiteral)
            .Select(field => $"{field.DeclaringType?.FullName}.{field.Name}")
            .ToArray();

        Assert.Empty(fields);
    }

    private static Type? TypeNamed(string name) =>
        AllAssemblies.Select(assembly => assembly.GetType(name)).FirstOrDefault(type => type is not null);

    private static Assembly AssemblyNamed(string name) =>
        AllAssemblies.Single(assembly => string.Equals(assembly.GetName().Name, name, StringComparison.Ordinal));
}
