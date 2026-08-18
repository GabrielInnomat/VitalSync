using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BuildingBlocks.Infrastructure.DependencyInjection.Extensibility;
using BuildingBlocks.Infrastructure.DependencyInjection.Registration;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging.Transport;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

public sealed class BuildingBlocksOptions
{
    public const int LoggingBehaviorOrder = 0;

    public const int ExceptionToResultBehaviorOrder = 100;

    public const int UnitOfWorkBehaviorOrder = 300;

    private readonly DomainEventCatalog _domainEvents = new();
    private readonly HandlerRegistrar _handlers;
    private readonly PersistenceRegistrar _persistence;
    private readonly MessagingRegistrar _messaging;
    private readonly ProvisioningRegistrar _provisioning;

    internal BuildingBlocksOptions(IServiceCollection services, PipelineBehaviorRegistry behaviorRegistry)
    {
        _handlers = new HandlerRegistrar(services, behaviorRegistry);
        _persistence = new PersistenceRegistrar(services, Wiring.Persistence, Wiring.Provisioning, Wiring.Runtime);
        _messaging = new MessagingRegistrar(services, Wiring.Messaging, Wiring.Provisioning, Wiring.Runtime);
        _provisioning = new ProvisioningRegistrar(Wiring.Provisioning);
    }

    internal BuildingBlocksWiringSettings Wiring { get; } = new();

    public RuntimeActivation Runtime => Wiring.Runtime;

    internal IReadOnlyCollection<Assembly> ScannedAssemblies => _handlers.ScannedAssemblies;

    internal IReadOnlyCollection<Assembly> DomainEventAssemblies => _domainEvents.Assemblies;

    internal DomainEventTypeRegistry DomainEventTypeRegistry => _domainEvents.Registry;

    [RequiresUnreferencedCode(TrimmingMessages.AssemblyScanning)]
    public BuildingBlocksOptions AddHandlersFrom(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        _handlers.AddFrom(assembly);
        return this;
    }

    [RequiresUnreferencedCode(TrimmingMessages.AssemblyScanning)]
    public BuildingBlocksOptions AddDomainEventsFrom(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        _domainEvents.Add(assembly);
        return this;
    }

    [RequiresUnreferencedCode(TrimmingMessages.AssemblyScanning)]
    public BuildingBlocksOptions AddPipelineBehavior(Type openGenericBehavior, int order)
    {
        ArgumentNullException.ThrowIfNull(openGenericBehavior);

        _handlers.AddPipelineBehavior(openGenericBehavior, order);
        return this;
    }

    public BuildingBlocksOptions UseNoPersistence()
    {
        _persistence.UseNone();
        return this;
    }

    public BuildingBlocksOptions UsePersistence(IPersistenceAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        _persistence.Use(adapter);
        return this;
    }

    public BuildingBlocksOptions WithoutEventHistory()
    {
        Wiring.Persistence.WaiveEventHistory();
        return this;
    }

    public BuildingBlocksOptions UseMessagingTransport(IMessagingTransportAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        _messaging.UseTransport(adapter);
        return this;
    }

    public BuildingBlocksOptions SubscribeToIntegrationEvents(
        string endpointName,
        Assembly consumerAssembly,
        params string[] topicPatterns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);
        ArgumentNullException.ThrowIfNull(consumerAssembly);
        ArgumentNullException.ThrowIfNull(topicPatterns);

        _messaging.Subscribe(endpointName, consumerAssembly, topicPatterns);
        return this;
    }

    public BuildingBlocksOptions ProvisionInfrastructure(InfrastructureProvisioning provisioning)
    {
        if (!Enum.IsDefined(provisioning))
        {
            throw new ArgumentOutOfRangeException(nameof(provisioning), provisioning, null);
        }

        _provisioning.Select(provisioning);
        return this;
    }
}
