using System.Reflection;
using GaWeCodes.DependencyInjection;
using GaWeCodes.DependencyInjection.Wiring;
using GaWeCodes.Persistence;
using GaWeCodes.Persistence.EventSourced;
using GaWeCodes.Persistence.StateStored;
using GaWeCodes.ReadModels;
using GaWeCodes.Schema;
using GaWeCodes.Startup;

namespace GaWeCodes.Tests;

public sealed class PublicSurfaceTests
{
    private static readonly Assembly Core = typeof(ServiceCollectionExtensions).Assembly;

    private static readonly Assembly RuntimeWolverine = typeof(WolverineRuntimeRegistration).Assembly;

    private static readonly Assembly Adapters = typeof(ReadModelRebuildWriter).Assembly;

    private static readonly Assembly Postgres = typeof(PostgresTransientFaults).Assembly;

    private static readonly Assembly EfCore = typeof(IEfCoreDatabaseDriver).Assembly;

    private static readonly Assembly EfCorePostgres = typeof(EfCorePersistenceOptionsExtensions).Assembly;

    private static readonly Assembly Marten = typeof(MartenPersistenceOptionsExtensions).Assembly;

    private static readonly Assembly RabbitMq = typeof(RabbitMqMessagingExtensions).Assembly;

    private static readonly Assembly Testing = typeof(PersistedSchema).Assembly;

    private static readonly Assembly[] AllAssemblies =
        [Core, RuntimeWolverine, Adapters, Postgres, EfCore, EfCorePostgres, Marten, RabbitMq, Testing];

    private static readonly string[] IntendedCoreApi =
    [
        "GaWeCodes.DependencyInjection.BuildingBlocksOptions",
        "GaWeCodes.DependencyInjection.HostApplicationBuilderExtensions",
        "GaWeCodes.DependencyInjection.InfrastructureProvisioning",
        "GaWeCodes.DependencyInjection.ServiceCollectionExtensions",
        "GaWeCodes.Persistence.EntityKeyJsonOptions",
        "GaWeCodes.Persistence.IPersistenceFaultTranslator",
        "GaWeCodes.Startup.IStartupCheck",
        "GaWeCodes.Startup.StartupPhase",
    ];

    private static readonly string[] IntendedCoreAdapterContract =
    [
        "GaWeCodes.DependencyInjection.Extensibility.IRuntimeActivator",
        "GaWeCodes.DependencyInjection.Extensibility.IWiringSnapshot",
        "GaWeCodes.DependencyInjection.Extensibility.RuntimeActivation",
        "GaWeCodes.DependencyInjection.Wiring.IntegrationEventSubscription",
        "GaWeCodes.Messaging.DomainEvents.DomainEventMetadataFactory",
        "GaWeCodes.Messaging.IntegrationEvents.TopicPatternMatcher",
        "GaWeCodes.Messaging.IntegrationEvents.TopicResolver",
        "GaWeCodes.Messaging.Transport.IMessageEmitter",
        "GaWeCodes.Messaging.Transport.IMessagingTransportAdapter",
        "GaWeCodes.Messaging.Transport.MessagingTransportRegistrationContext",
        "GaWeCodes.Persistence.AggregateFactory",
        "GaWeCodes.Persistence.AggregateStyle",
        "GaWeCodes.Persistence.EntityKeyActivator",
        "GaWeCodes.Persistence.IPersistenceAdapter",
        "GaWeCodes.Persistence.PersistenceRegistrationContext",
        "GaWeCodes.Startup.SynchronousStartupCheck",
    ];

    private static readonly string[] IntendedTestingApi =
    [
        "GaWeCodes.Schema.PersistedSchema",
    ];

    private static readonly string[] IntendedRuntimeWolverineApi =
    [
        "GaWeCodes.DependencyInjection.Wiring.WolverineRuntimeActivator",
        "GaWeCodes.DependencyInjection.Wiring.WolverineRuntimeRegistration",
        "GaWeCodes.DependencyInjection.WolverineRuntimeOptionsExtensions",
        "GaWeCodes.Diagnostics.DeadLetterHealthCheckRegistration",
        "GaWeCodes.Messaging.Transport.IWolverineMessagingTransport",
        "GaWeCodes.Persistence.IOutboxDurabilityConfigurator",
    ];

    private static readonly string[] IntendedAdaptersApi =
    [
        "GaWeCodes.Persistence.AggregateTracker`1",
        "GaWeCodes.Persistence.DomainEventEnvelopeFactory",
        "GaWeCodes.Persistence.EntityKeyFormatter",
        "GaWeCodes.Persistence.ITrackedAggregate",
        "GaWeCodes.Persistence.PersistenceFailureCodes",
        "GaWeCodes.ReadModels.ReadModelRebuildWriter",
    ];

    private static readonly string[] IntendedPostgresApi =
    [
        "GaWeCodes.Persistence.PostgresFaultTranslator",
        "GaWeCodes.Persistence.PostgresTransientFaults",
    ];

    private static readonly string[] IntendedEfCoreApi =
    [
        "GaWeCodes.Persistence.EntityKeyModelBuilderExtensions",
        "GaWeCodes.Persistence.StateStored.EfCorePersistenceAdapter`1",
        "GaWeCodes.Persistence.StateStored.IEfCoreDatabaseDriver",
        "GaWeCodes.ReadModels.StateStoredReadModelRebuildRunner`1",
    ];

    private static readonly string[] IntendedEfCorePostgresApi =
    [
        "GaWeCodes.Persistence.StateStored.EfCorePersistenceOptionsExtensions",
    ];

    private static readonly string[] IntendedMartenApi =
    [
        "GaWeCodes.Persistence.EventSourced.MartenPersistenceOptionsExtensions",
        "GaWeCodes.ReadModels.EventSourcedReadModelRebuildRunner",
    ];

    private static readonly string[] IntendedRabbitMqApi =
    [
        "GaWeCodes.DependencyInjection.RabbitMqMessagingExtensions",
    ];

    private static readonly string[] ExtensionPoints =
    [
        "GaWeCodes.DependencyInjection.Extensibility.IRuntimeActivator",
        "GaWeCodes.DependencyInjection.Extensibility.IWiringSnapshot",
        "GaWeCodes.Messaging.Transport.IMessagingTransportAdapter",
        "GaWeCodes.Messaging.Transport.IWolverineMessagingTransport",
        "GaWeCodes.Persistence.AggregateStyle",
        "GaWeCodes.Persistence.IOutboxDurabilityConfigurator",
        "GaWeCodes.Persistence.IPersistenceAdapter",
        "GaWeCodes.Persistence.IPersistenceFaultTranslator",
        "GaWeCodes.Startup.IStartupCheck",
        "GaWeCodes.Startup.StartupPhase",
        "GaWeCodes.Startup.SynchronousStartupCheck",
    ];

    private static readonly string[] RequiredByWolverineCodeGeneration =
    [
        "GaWeCodes.Messaging.DomainEvents.DomainEventEnvelope",
        "GaWeCodes.Messaging.DomainEvents.DomainEventEnvelopeHandler",
        "GaWeCodes.Messaging.DomainEvents.DomainEventEnvelopeSerializer",
        "GaWeCodes.Messaging.DomainEvents.DomainEventTypeRegistry",
        "GaWeCodes.Messaging.DomainEvents.ProjectionEnvelope",
        "GaWeCodes.Messaging.DomainEvents.ProjectionEnvelopeHandler",
        "GaWeCodes.Messaging.DomainEvents.ProjectionRunner",
        "GaWeCodes.Messaging.IntegrationEvents.IIntegrationEventSinkFactory",
        "GaWeCodes.Messaging.IntegrationEvents.IntegrationEventSourceContext",
        "GaWeCodes.Messaging.IntegrationEvents.OwnContextIntegrationEventFilter",
    ];

    private static readonly string[] CodeGenerationTypesInTheCore =
    [
        "GaWeCodes.Messaging.DomainEvents.DomainEventEnvelope",
        "GaWeCodes.Messaging.DomainEvents.DomainEventEnvelopeSerializer",
        "GaWeCodes.Messaging.DomainEvents.DomainEventTypeRegistry",
        "GaWeCodes.Messaging.DomainEvents.ProjectionEnvelope",
        "GaWeCodes.Messaging.DomainEvents.ProjectionRunner",
        "GaWeCodes.Messaging.IntegrationEvents.IIntegrationEventSinkFactory",
        "GaWeCodes.Messaging.IntegrationEvents.IntegrationEventSourceContext",
    ];

    private static readonly string[] CodeGenerationTypesInTheWolverineRuntime =
    [
        "GaWeCodes.Messaging.DomainEvents.DomainEventEnvelopeHandler",
        "GaWeCodes.Messaging.DomainEvents.ProjectionEnvelopeHandler",
        "GaWeCodes.Messaging.IntegrationEvents.OwnContextIntegrationEventFilter",
    ];

    public static TheoryData<string, string[]> PinnedSurfaces =>
        new()
        {
            {
                "GaWeCodes.Composition",
                [.. IntendedCoreApi
                    .Concat(IntendedCoreAdapterContract)
                    .Concat(CodeGenerationTypesInTheCore)]
            },
            { "GaWeCodes.Testing", IntendedTestingApi },
            {
                "GaWeCodes.Runtime.Wolverine",
                [.. IntendedRuntimeWolverineApi.Concat(CodeGenerationTypesInTheWolverineRuntime)]
            },
            { "GaWeCodes.Persistence", IntendedAdaptersApi },
            { "GaWeCodes.Persistence.Postgres", IntendedPostgresApi },
            { "GaWeCodes.Persistence.EfCore", IntendedEfCoreApi },
            { "GaWeCodes.Persistence.EfCore.Postgres", IntendedEfCorePostgresApi },
            { "GaWeCodes.EventSourcing.Marten", IntendedMartenApi },
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
            .Concat(IntendedRuntimeWolverineApi)
            .Concat(IntendedAdaptersApi)
            .Concat(IntendedPostgresApi)
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
        var satellites = new[] { RuntimeWolverine, Adapters, EfCore, Marten, RabbitMq }
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
