using System.Collections.Concurrent;
using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Events;

/// <summary>
/// Dispatches a committed domain event to the in-context projection handlers registered for its type.
/// </summary>
/// <remarks>
/// The runner resolves every <see cref="IProjectionHandler{TDomainEvent}"/> registered for the event's runtime type
/// and invokes them sequentially. Idempotency is the handler's responsibility, tracked via the event's stable
/// <see cref="IDomainEvent.EventId"/> (ADR-0022). Per-aggregate ordering is preserved by the messaging transport's
/// durable, sequential local queue (see <c>WolverineOptionsExtensions.ApplyBuildingBlockDomainEventRouting</c>). Only
/// this plumbing lives in Infrastructure — the handler contract lives in <c>BuildingBlocks.Application</c> and the
/// read models belong to each service.
/// </remarks>
/// <param name="serviceProvider">The scoped service provider used to resolve the projection handlers.</param>
public sealed class ProjectionRunner(IServiceProvider serviceProvider)
{
    private static readonly ConcurrentDictionary<Type, ProjectionInvoker> Invokers = new();

    /// <summary>
    /// Invokes all projection handlers registered for the domain event's runtime type.
    /// </summary>
    /// <param name="domainEvent">The committed domain event to project.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous projection run.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.</exception>
    public Task RunAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var invoker = Invokers.GetOrAdd(
            domainEvent.GetType(),
            static type => (ProjectionInvoker)Activator.CreateInstance(
                typeof(ProjectionInvoker<>).MakeGenericType(type))!);

        return invoker.InvokeAsync(domainEvent, serviceProvider, cancellationToken);
    }

    private abstract class ProjectionInvoker
    {
        public abstract Task InvokeAsync(IDomainEvent domainEvent, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class ProjectionInvoker<TDomainEvent> : ProjectionInvoker
        where TDomainEvent : IDomainEvent
    {
        public override async Task InvokeAsync(IDomainEvent domainEvent, IServiceProvider services, CancellationToken cancellationToken)
        {
            var typedEvent = (TDomainEvent)domainEvent;
            foreach (var handler in services.GetServices<IProjectionHandler<TDomainEvent>>())
            {
                await handler.Handle(typedEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
