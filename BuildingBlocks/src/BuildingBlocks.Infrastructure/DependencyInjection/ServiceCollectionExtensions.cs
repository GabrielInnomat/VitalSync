using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Events;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// The public DI surface of <c>BuildingBlocks.Infrastructure</c> for service hosts.
/// </summary>
/// <remarks>
/// Hosts call <see cref="AddBuildingBlocks"/> once at composition time; everything else in this package is reached
/// through the <c>Domain</c>/<c>Application</c> abstractions. The registration wires the dispatcher, the pipeline
/// behaviors in the canonical order, the outbox-backed publisher with its projection runner, the default UTC clock,
/// and a no-op messaging transport that <see cref="BuildingBlocksOptions.UseWolverineMessaging"/> replaces. The outbox itself is Wolverine's
/// own transactional outbox (ADR-0023); the host wires it up via <see cref="WolverineOptionsExtensions"/> from its
/// <c>UseWolverine</c> setup, and <see cref="DomainEventEnvelopeHandler"/> is the single handler that delivers into
/// the publisher registered here.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Building Blocks platform services and applies the host's capability selection.
    /// </summary>
    /// <remarks>
    /// Pipeline behaviors are registered in the canonical order mandated by ADR-0015/0017 — exception-to-result
    /// translation first, then logging, then the unit of work closest to the handler — and execute in exactly that
    /// registration order.
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">The callback that selects handlers, persistence style, and messaging via <see cref="BuildingBlocksOptions"/>.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton<IIntegrationEventTransport, NullIntegrationEventTransport>();
        configure(new BuildingBlocksOptions(services));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddScoped<ISender, Sender>();
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ExceptionToResultBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>)));

        services.TryAddScoped<ProjectionRunner>();
        services.TryAddScoped<IDomainEventPublisher, Publisher>();

        return services;
    }
}
