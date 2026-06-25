using BuildingBlocks.Domain.Events;
using BuildingBlocks.EventProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.EventProcessing;

/// <summary>
/// A domain event dispatcher that resolves
/// <see cref="IDomainEventHandler{TDomainEvent}"/> instances from the DI
/// container and invokes each one for every supplied event.
/// </summary>
public sealed class DependencyInjectionDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes the dispatcher with a service provider.</summary>
    public DependencyInjectionDomainEventDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var task = (Task)handlerType
                    .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!
                    .Invoke(handler, new object[] { domainEvent, cancellationToken })!;

                await task;
            }
        }
    }
}