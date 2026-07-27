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
/// and invokes them sequentially, forwarding the event's stream position so handlers can track a last-processed marker
/// (ADR-0022). Per-aggregate ordering is preserved because the drain loop feeds events of one stream in order;
/// idempotency remains the handler's responsibility. Only this plumbing lives in Infrastructure — the handler
/// contract lives in <c>BuildingBlocks.Application</c> and the read models belong to each service.
/// </remarks>
/// <param name="serviceProvider">The scoped service provider used to resolve the projection handlers.</param>
public sealed class ProjectionRunner(IServiceProvider serviceProvider)
{
    private static readonly ConcurrentDictionary<Type, ProjectionInvoker> Invokers = new();

    /// <summary>
    /// Invokes all projection handlers registered for the domain event's runtime type.
    /// </summary>
    /// <param name="domainEvent">The committed domain event to project.</param>
    /// <param name="streamPosition">The event's position (version) within its aggregate's stream.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous projection run.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.</exception>
    public Task RunAsync(IDomainEvent domainEvent, long streamPosition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var invoker = Invokers.GetOrAdd(
            domainEvent.GetType(),
            static type => (ProjectionInvoker)Activator.CreateInstance(
                typeof(ProjectionInvoker<>).MakeGenericType(type))!);

        return invoker.InvokeAsync(domainEvent, streamPosition, serviceProvider, cancellationToken);
    }

    private abstract class ProjectionInvoker
    {
        public abstract Task InvokeAsync(IDomainEvent domainEvent, long streamPosition, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class ProjectionInvoker<TDomainEvent> : ProjectionInvoker
        where TDomainEvent : IDomainEvent
    {
        public override async Task InvokeAsync(IDomainEvent domainEvent, long streamPosition, IServiceProvider services, CancellationToken cancellationToken)
        {
            var typedEvent = (TDomainEvent)domainEvent;
            foreach (var handler in services.GetServices<IProjectionHandler<TDomainEvent>>())
            {
                await handler.Handle(typedEvent, streamPosition, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
