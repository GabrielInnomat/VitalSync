using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Events;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        AddBuildingBlocksCore(services, configure);
        return services;
    }

    internal static WolverineWiringSettings AddBuildingBlocksCore(IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var behaviorRegistry = new PipelineBehaviorRegistry();
        services.TryAddSingleton(behaviorRegistry);

        services.TryAddSingleton<IIntegrationEventSinkFactory, NullIntegrationEventSinkFactory>();
        var options = new BuildingBlocksOptions(services, behaviorRegistry);
        configure(options);

        if (options.WolverineWiring.Subscription is not null && options.WolverineWiring.RabbitMqUri is null)
        {
            throw new InvalidOperationException(
                "SubscribeToIntegrationEvents was selected without UseWolverineMessaging. Subscribing declares a " +
                "queue on the RabbitMQ broker and binds it to the platform exchange, so the transport must be " +
                "configured as well (ADR-0023).");
        }

        var domainEventTypeRegistry = options.DomainEventTypeRegistry;

        if (options.WolverineWiring.ApplyDomainEventRouting && domainEventTypeRegistry.NamesByType.Count == 0)
        {
            throw new InvalidOperationException(
                "A persistence strategy was configured but no domain event assembly was registered. Every domain " +
                "event is written to the outbox under the name from its [EventName], so the names must be known " +
                "before the first commit: call options.AddDomainEventsFrom(typeof(SomeDomainEvent).Assembly).");
        }

        services.TryAddSingleton(domainEventTypeRegistry);
        services.TryAddSingleton<DomainEventEnvelopeSerializer>();

        if (options.ValidateHandlersOnStart)
        {
            services.AddHostedService(provider =>
                new HandlerRegistrationStartupValidator(provider, options.ScannedAssemblies));
        }

        if (options.ValidateWolverineOnStart && options.WolverineWiring.RequiresWolverine)
        {
            services.AddHostedService<WolverineWiringStartupValidator>();
        }

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IUnitOfWork)))
        {
            services.AddHostedService<MissingUnitOfWorkStartupLogger>();
        }

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

        return options.WolverineWiring;
    }
}
