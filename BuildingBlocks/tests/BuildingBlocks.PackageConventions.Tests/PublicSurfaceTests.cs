using System.Reflection;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
using BuildingBlocks.Infrastructure.ReadModels;
using BuildingBlocks.Infrastructure.Startup;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class PublicSurfaceTests
{
    private static readonly Assembly Core = typeof(ServiceCollectionExtensions).Assembly;

    private static readonly Assembly RuntimeWolverine = typeof(WolverineRuntimeRegistration).Assembly;

    private static readonly Assembly Adapters = typeof(ReadModelRebuildWriter).Assembly;

    private static readonly Assembly EfCore = typeof(EfCorePersistenceOptionsExtensions).Assembly;

    private static readonly Assembly Marten = typeof(MartenPersistenceOptionsExtensions).Assembly;

    private static readonly Assembly RabbitMq = typeof(RabbitMqMessagingExtensions).Assembly;

    private static readonly Assembly[] AllAssemblies =
        [Core, RuntimeWolverine, Adapters, EfCore, Marten, RabbitMq];

    private static readonly string[] IntendedCoreApi =
    [
        "BuildingBlocks.Infrastructure.DependencyInjection.BuildingBlocksOptions",
        "BuildingBlocks.Infrastructure.DependencyInjection.HostApplicationBuilderExtensions",
        "BuildingBlocks.Infrastructure.DependencyInjection.InfrastructureProvisioning",
        "BuildingBlocks.Infrastructure.DependencyInjection.ServiceCollectionExtensions",
        "BuildingBlocks.Infrastructure.Persistence.EntityKeyJsonOptions",
        "BuildingBlocks.Infrastructure.Persistence.IPersistenceFaultTranslator",
        "BuildingBlocks.Infrastructure.Startup.IStartupCheck",
        "BuildingBlocks.Infrastructure.Startup.StartupPhase",
    ];

    private static readonly string[] IntendedCoreAdapterContract =
    [
        "BuildingBlocks.Infrastructure.DependencyInjection.Extensibility.IRuntimeActivator",
        "BuildingBlocks.Infrastructure.DependencyInjection.Extensibility.IWiringSnapshot",
        "BuildingBlocks.Infrastructure.DependencyInjection.Extensibility.RuntimeActivation",
        "BuildingBlocks.Infrastructure.DependencyInjection.Wiring.IntegrationEventSubscription",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventMetadataFactory",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.TopicPatternMatcher",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.TopicResolver",
        "BuildingBlocks.Infrastructure.Messaging.Transport.IMessageEmitter",
        "BuildingBlocks.Infrastructure.Messaging.Transport.IMessagingTransportAdapter",
        "BuildingBlocks.Infrastructure.Messaging.Transport.MessagingTransportRegistrationContext",
        "BuildingBlocks.Infrastructure.Persistence.AggregateFactory",
        "BuildingBlocks.Infrastructure.Persistence.AggregateStyle",
        "BuildingBlocks.Infrastructure.Persistence.EntityKeyActivator",
        "BuildingBlocks.Infrastructure.Persistence.IPersistenceAdapter",
        "BuildingBlocks.Infrastructure.Persistence.PersistenceRegistrationContext",
        "BuildingBlocks.Infrastructure.Startup.SynchronousStartupCheck",
    ];

    private static readonly string[] IntendedTestingApi =
    [
        "BuildingBlocks.Infrastructure.Schema.PersistedSchema",
    ];

    private static readonly string[] IntendedRuntimeWolverineApi =
    [
        "BuildingBlocks.Infrastructure.DependencyInjection.Wiring.WolverineRuntimeActivator",
        "BuildingBlocks.Infrastructure.DependencyInjection.Wiring.WolverineRuntimeRegistration",
        "BuildingBlocks.Infrastructure.DependencyInjection.WolverineRuntimeOptionsExtensions",
        "BuildingBlocks.Infrastructure.Diagnostics.DeadLetterHealthCheckRegistration",
        "BuildingBlocks.Infrastructure.Messaging.Transport.IWolverineMessagingTransport",
        "BuildingBlocks.Infrastructure.Persistence.IOutboxDurabilityConfigurator",
    ];

    private static readonly string[] IntendedAdaptersApi =
    [
        "BuildingBlocks.Infrastructure.Persistence.AggregateTracker`1",
        "BuildingBlocks.Infrastructure.Persistence.DomainEventEnvelopeFactory",
        "BuildingBlocks.Infrastructure.Persistence.EntityKeyFormatter",
        "BuildingBlocks.Infrastructure.Persistence.ITrackedAggregate",
        "BuildingBlocks.Infrastructure.Persistence.PersistenceFailureCodes",
        "BuildingBlocks.Infrastructure.Persistence.PostgresFaultTranslator",
        "BuildingBlocks.Infrastructure.Persistence.PostgresTransientFaults",
        "BuildingBlocks.Infrastructure.ReadModels.ReadModelRebuildWriter",
    ];

    private static readonly string[] IntendedEfCoreApi =
    [
        "BuildingBlocks.Infrastructure.Persistence.EntityKeyModelBuilderExtensions",
        "BuildingBlocks.Infrastructure.Persistence.StateStored.EfCorePersistenceOptionsExtensions",
        "BuildingBlocks.Infrastructure.ReadModels.StateStoredReadModelRebuildRunner`1",
    ];

    private static readonly string[] IntendedMartenApi =
    [
        "BuildingBlocks.Infrastructure.Persistence.EventSourced.MartenPersistenceOptionsExtensions",
        "BuildingBlocks.Infrastructure.ReadModels.EventSourcedReadModelRebuildRunner",
    ];

    private static readonly string[] IntendedRabbitMqApi =
    [
        "BuildingBlocks.Infrastructure.DependencyInjection.RabbitMqMessagingExtensions",
    ];

    private static readonly string[] ExtensionPoints =
    [
        "BuildingBlocks.Infrastructure.DependencyInjection.Extensibility.IRuntimeActivator",
        "BuildingBlocks.Infrastructure.DependencyInjection.Extensibility.IWiringSnapshot",
        "BuildingBlocks.Infrastructure.Messaging.Transport.IMessagingTransportAdapter",
        "BuildingBlocks.Infrastructure.Messaging.Transport.IWolverineMessagingTransport",
        "BuildingBlocks.Infrastructure.Persistence.AggregateStyle",
        "BuildingBlocks.Infrastructure.Persistence.IOutboxDurabilityConfigurator",
        "BuildingBlocks.Infrastructure.Persistence.IPersistenceAdapter",
        "BuildingBlocks.Infrastructure.Persistence.IPersistenceFaultTranslator",
        "BuildingBlocks.Infrastructure.Startup.IStartupCheck",
        "BuildingBlocks.Infrastructure.Startup.StartupPhase",
        "BuildingBlocks.Infrastructure.Startup.SynchronousStartupCheck",
    ];

    private static readonly string[] RequiredByWolverineCodeGeneration =
    [
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelope",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelopeHandler",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelopeSerializer",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventTypeRegistry",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionEnvelope",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionEnvelopeHandler",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionRunner",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.IIntegrationEventSinkFactory",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.IntegrationEventSourceContext",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.OwnContextIntegrationEventFilter",
    ];

    private static readonly string[] CodeGenerationTypesInTheCore =
    [
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelope",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelopeSerializer",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventTypeRegistry",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionEnvelope",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionRunner",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.IIntegrationEventSinkFactory",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.IntegrationEventSourceContext",
    ];

    private static readonly string[] CodeGenerationTypesInTheWolverineRuntime =
    [
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelopeHandler",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionEnvelopeHandler",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.OwnContextIntegrationEventFilter",
    ];

    public static TheoryData<string, string[]> PinnedSurfaces =>
        new()
        {
            {
                "BuildingBlocks.Infrastructure",
                [.. IntendedCoreApi
                    .Concat(IntendedCoreAdapterContract)
                    .Concat(IntendedTestingApi)
                    .Concat(CodeGenerationTypesInTheCore)]
            },
            {
                "BuildingBlocks.Runtime.Wolverine",
                [.. IntendedRuntimeWolverineApi.Concat(CodeGenerationTypesInTheWolverineRuntime)]
            },
            { "BuildingBlocks.Persistence.Adapters", IntendedAdaptersApi },
            { "BuildingBlocks.Persistence.EfCore.Postgres", IntendedEfCoreApi },
            { "BuildingBlocks.EventSourcing.Marten", IntendedMartenApi },
            { "BuildingBlocks.Messaging.Wolverine.RabbitMq", IntendedRabbitMqApi },
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
            .Concat(IntendedEfCoreApi)
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
