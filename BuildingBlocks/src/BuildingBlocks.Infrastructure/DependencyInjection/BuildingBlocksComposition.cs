using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection.Extensibility;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Startup;
using BuildingBlocks.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

internal static class BuildingBlocksComposition
{
    public static BuildingBlocksWiringSettings Compose(IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        EnsureSingleCall(services);

        var behaviorRegistry = new PipelineBehaviorRegistry();
        var options = Configure(services, behaviorRegistry, configure);

        Validate(options);
        RegisterCore(services, options);
        ValidateBehaviorOrders(services, behaviorRegistry);
        RegisterStartupChecks(services, options);

        return options.Wiring;
    }

    private static void EnsureSingleCall(IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(BuildingBlocksRegistrationMarker)))
        {
            throw new InvalidOperationException(
                "AddBuildingBlocks was called more than once on the same service collection. The behavior " +
                "registry, the Wolverine wiring settings and the domain event names are one shared object each, " +
                "registered by the first call; a second call would fill fresh instances that are never resolved, " +
                "so its behaviors would run at order 0, its persistence and messaging selection would be ignored " +
                "and its [EventName] names would be missing at the first commit. Make every selection in a single " +
                "AddBuildingBlocks callback.");
        }

        services.AddSingleton(new BuildingBlocksRegistrationMarker());
    }

    private static BuildingBlocksOptions Configure(
        IServiceCollection services,
        PipelineBehaviorRegistry behaviorRegistry,
        Action<BuildingBlocksOptions> configure)
    {
        services.AddSingleton(behaviorRegistry);
        services.TryAddSingleton<IIntegrationEventSinkFactory, NullIntegrationEventSinkFactory>();

        var options = new BuildingBlocksOptions(services, behaviorRegistry);
        configure(options);
        return options;
    }

    private static void Validate(BuildingBlocksOptions options)
    {
        var wiring = options.Wiring;

        if (wiring.Messaging.Subscription is not null && wiring.Messaging.Transport is null)
        {
            throw new InvalidOperationException(
                "SubscribeToIntegrationEvents was selected without UseWolverineMessaging. Subscribing declares a " +
                "queue on the RabbitMQ broker and binds it to the platform exchange, so the transport must be " +
                "configured as well.");
        }

        if (wiring.Messaging.IsSelected && !wiring.Persistence.IsSelected)
        {
            throw new InvalidOperationException(
                "UseWolverineMessaging was selected without a persistence strategy. Integration events are sent " +
                "through a durable endpoint so that they survive a broker restart and a crash between commit and " +
                "broker acknowledgement, and a durable endpoint needs Wolverine's message store. " +
                "Without one the host would look durable and silently not be. Select UseEfCorePersistence<TContext>" +
                "(writeConnectionString) or UseMartenEventSourcing(writeConnectionString) as well.");
        }

        if (!wiring.Persistence.IsSelected)
        {
            return;
        }

        if (options.DomainEventTypeRegistry.NamesByType.Count == 0)
        {
            throw new InvalidOperationException(
                "A persistence strategy was configured but no domain event assembly was registered. Every domain " +
                "event is written to the outbox under the name from its [EventName], so the names must be known " +
                "before the first commit: call options.AddDomainEventsFrom(typeof(SomeDomainEvent).Assembly).");
        }

        AggregateFactory.EnsureAggregatesAreReconstitutable(options.DomainEventAssemblies);
    }

    private static void ValidateBehaviorOrders(IServiceCollection services, PipelineBehaviorRegistry behaviorRegistry)
    {
        foreach (var descriptor in services)
        {
            var serviceType = descriptor.ServiceType;
            if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(IPipelineBehavior<,>))
            {
                continue;
            }

            var implementationType = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
            if (implementationType is not null && behaviorRegistry.TryGetOrder(implementationType, out _))
            {
                continue;
            }

            var name = implementationType?.ToString() ?? $"a factory-registered behavior for '{serviceType}'";

            throw new InvalidOperationException(
                $"The pipeline behavior {name} was added to the service collection directly and therefore has no " +
                "order. An unordered behavior would run at order 0, which is the order of the logging behavior, so " +
                "it would silently collide with it. Register it with " +
                "options.AddPipelineBehavior(typeof(MyBehavior<,>), order) instead.");
        }
    }

    private static void RegisterCore(IServiceCollection services, BuildingBlocksOptions options)
    {
        services.AddSingleton(options.DomainEventTypeRegistry);
        services.TryAddSingleton<DomainEventEnvelopeSerializer>();

        services.AddSingleton(options.Wiring);
        services.AddSingleton<IWiringSnapshot>(options.Wiring);
        services.AddSingleton(options.Wiring.Persistence);
        services.AddSingleton(options.Wiring.Messaging);
        services.AddSingleton(options.Wiring.Provisioning);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddScoped<ISender, RequestSender>();

        options.AddPipelineBehavior(typeof(LoggingBehavior<,>), BuildingBlocksOptions.LoggingBehaviorOrder);
        options.AddPipelineBehavior(typeof(ExceptionToResultBehavior<,>), BuildingBlocksOptions.ExceptionToResultBehaviorOrder);
        options.AddPipelineBehavior(typeof(UnitOfWorkBehavior<,>), BuildingBlocksOptions.UnitOfWorkBehaviorOrder);

        services.TryAddScoped<ProjectionRunner>();
        services.TryAddScoped<MapperRunner>();
        services.TryAddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
        services.TryAddScoped<IUnitOfWork, NullUnitOfWork>();
    }

    private static void RegisterStartupChecks(IServiceCollection services, BuildingBlocksOptions options)
    {
        services.AddHostedService<StartupCheckRunner>();

        services.AddSingleton<IStartupCheck>(provider =>
            new HandlerRegistrationCheck(provider, options.ScannedAssemblies));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupCheck, IntegrationEventMapperCheck>());
        services.AddSingleton<IStartupCheck>(provider => new UnitOfWorkPresenceCheck(
            provider,
            options.Wiring.Persistence,
            options.ScannedAssemblies,
            provider.GetRequiredService<ILogger<UnitOfWorkPresenceCheck>>()));
    }
}
