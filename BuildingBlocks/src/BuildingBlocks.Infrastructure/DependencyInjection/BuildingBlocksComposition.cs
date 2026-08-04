using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Events;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

internal static class BuildingBlocksComposition
{
    public static WolverineWiringSettings Compose(IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        var options = Configure(services, configure);

        Validate(options);
        RegisterCore(services, options);
        RegisterStartupChecks(services, options);

        return options.WolverineWiring;
    }

    private static BuildingBlocksOptions Configure(IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        var behaviorRegistry = new PipelineBehaviorRegistry();
        services.TryAddSingleton(behaviorRegistry);
        services.TryAddSingleton<IIntegrationEventSinkFactory, NullIntegrationEventSinkFactory>();

        var options = new BuildingBlocksOptions(services, behaviorRegistry);
        configure(options);
        return options;
    }

    private static void Validate(BuildingBlocksOptions options)
    {
        var wiring = options.WolverineWiring;

        if (wiring.Subscription is not null && wiring.Messaging is null)
        {
            throw new InvalidOperationException(
                "SubscribeToIntegrationEvents was selected without UseWolverineMessaging. Subscribing declares a " +
                "queue on the RabbitMQ broker and binds it to the platform exchange, so the transport must be " +
                "configured as well (ADR-0023).");
        }

        if (wiring.Messaging is not null && !wiring.Persistence.IsSelected)
        {
            throw new InvalidOperationException(
                "UseWolverineMessaging was selected without a persistence strategy. Integration events are sent " +
                "through a durable endpoint so that they survive a broker restart and a crash between commit and " +
                "broker acknowledgement (ADR-0022/0023), and a durable endpoint needs Wolverine's message store. " +
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

    private static void RegisterCore(IServiceCollection services, BuildingBlocksOptions options)
    {
        services.TryAddSingleton(options.DomainEventTypeRegistry);
        services.TryAddSingleton<DomainEventEnvelopeSerializer>();

        services.TryAddSingleton(options.WolverineWiring);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IWolverineExtension, BuildingBlocksWolverineExtension>());

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddScoped<ISender, Sender>();

        options.AddPipelineBehavior(typeof(LoggingBehavior<,>), BuildingBlocksOptions.LoggingBehaviorOrder);
        options.AddPipelineBehavior(typeof(ExceptionToResultBehavior<,>), BuildingBlocksOptions.ExceptionToResultBehaviorOrder);
        options.AddPipelineBehavior(typeof(UnitOfWorkBehavior<,>), BuildingBlocksOptions.UnitOfWorkBehaviorOrder);

        services.TryAddScoped<ProjectionRunner>();
        services.TryAddScoped<IDomainEventPublisher, Publisher>();
    }

    private static void RegisterStartupChecks(IServiceCollection services, BuildingBlocksOptions options)
    {
        services.AddHostedService<StartupCheckRunner>();

        services.AddSingleton<IStartupCheck>(provider =>
            new HandlerRegistrationCheck(provider, options.ScannedAssemblies));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupCheck, WolverineRuntimeCheck>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupCheck, IntegrationEventSubscriptionCheck>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupCheck, UnitOfWorkPresenceCheck>());
    }
}
