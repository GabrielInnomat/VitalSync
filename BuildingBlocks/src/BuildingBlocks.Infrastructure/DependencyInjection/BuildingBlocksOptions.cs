using System.Reflection;
using BuildingBlocks.Infrastructure.DependencyInjection.Registration;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using Microsoft.EntityFrameworkCore;
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
        _persistence = new PersistenceRegistrar(services, Wiring.Persistence, Wiring.Provisioning);
        _messaging = new MessagingRegistrar(services, Wiring.Messaging);
        _provisioning = new ProvisioningRegistrar(Wiring.Provisioning);
    }

    internal BuildingBlocksWiringSettings Wiring { get; } = new();

    internal IReadOnlyCollection<Assembly> ScannedAssemblies => _handlers.ScannedAssemblies;

    internal IReadOnlyCollection<Assembly> DomainEventAssemblies => _domainEvents.Assemblies;

    internal DomainEventTypeRegistry DomainEventTypeRegistry => _domainEvents.Registry;

    public BuildingBlocksOptions AddHandlersFrom(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        _handlers.AddFrom(assembly);
        return this;
    }

    public BuildingBlocksOptions AddDomainEventsFrom(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        _domainEvents.Add(assembly);
        return this;
    }

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

    public BuildingBlocksOptions UseEfCorePersistence<TContext>(
        string connectionString,
        Action<DbContextOptionsBuilder>? configureContext = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        _persistence.UseEfCore<TContext>(connectionString, configureContext);
        return this;
    }

    public BuildingBlocksOptions UseMartenEventSourcing(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        _persistence.UseMarten(connectionString);
        return this;
    }

    public BuildingBlocksOptions UseWolverineMessaging(Uri rabbitMqUri, string exchangeName, string contextName)
    {
        ArgumentNullException.ThrowIfNull(rabbitMqUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        _messaging.UseMessaging(rabbitMqUri, exchangeName, contextName);
        return this;
    }

    public BuildingBlocksOptions SubscribeToIntegrationEvents(
        string queueName,
        Assembly consumerAssembly,
        params string[] topicPatterns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(consumerAssembly);
        ArgumentNullException.ThrowIfNull(topicPatterns);

        _messaging.Subscribe(queueName, consumerAssembly, topicPatterns);
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
