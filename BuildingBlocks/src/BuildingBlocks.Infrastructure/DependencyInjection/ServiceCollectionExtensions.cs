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

/// <summary>
/// The public DI surface of <c>BuildingBlocks.Infrastructure</c> for service hosts.
/// </summary>
/// <remarks>
/// Hosts call <see cref="AddBuildingBlocks"/> once at composition time; everything else in this package is reached
/// through the <c>Domain</c>/<c>Application</c> abstractions. The registration wires the dispatcher, the pipeline
/// behaviors with explicit orders, the outbox-backed publisher with its projection runner, the default UTC clock,
/// and a no-op integration-event sink factory that <see cref="BuildingBlocksOptions.UseWolverineMessaging"/> replaces. The outbox itself is Wolverine's
/// own transactional outbox (ADR-0023); its configuration is applied automatically by a registered
/// <see cref="BuildingBlocksWolverineExtension"/> when the host calls <c>UseWolverine</c> (ADR-0027), and
/// <see cref="DomainEventEnvelopeHandler"/> is the single handler that delivers into the publisher registered here.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Building Blocks platform services and applies the host's capability selection.
    /// </summary>
    /// <remarks>
    /// Pipeline behaviors are registered with explicit orders (ADR-0015): logging outermost
    /// (<see cref="BuildingBlocksOptions.LoggingBehaviorOrder"/>) so translated failures are logged as warnings and only
    /// unexpected exceptions as errors, then exception-to-result translation
    /// (<see cref="BuildingBlocksOptions.ExceptionToResultBehaviorOrder"/>), then the unit of work closest to the
    /// handler (<see cref="BuildingBlocksOptions.UnitOfWorkBehaviorOrder"/>). The sender wraps behaviors by ascending
    /// order; hosts add their own at a chosen position via <see cref="BuildingBlocksOptions.AddPipelineBehavior"/>.
    /// Unless the host sets <see cref="BuildingBlocksOptions.ValidateHandlersOnStart"/> to <see langword="false"/>, a
    /// startup hosted service is registered that verifies every command and query in the scanned assemblies resolves
    /// to a handler, failing the host at startup instead of on the first request. Likewise, unless
    /// <see cref="BuildingBlocksOptions.ValidateWolverineOnStart"/> is <see langword="false"/>, a startup check fails
    /// the host when a selected capability requires Wolverine but <c>UseWolverine</c> was never called (ADR-0027).
    /// When no <see cref="IUnitOfWork"/> is registered by the end of the call, a startup notice is logged that
    /// commands are dispatched without a commit — valid for tests and gateway hosts, a misconfiguration elsewhere.
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">The callback that selects handlers, persistence style, and messaging via <see cref="BuildingBlocksOptions"/>.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var behaviorRegistry = new PipelineBehaviorRegistry();
        services.TryAddSingleton(behaviorRegistry);

        services.TryAddSingleton<IIntegrationEventSinkFactory, NullIntegrationEventSinkFactory>();
        var options = new BuildingBlocksOptions(services, behaviorRegistry);
        configure(options);

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

        return services;
    }
}
