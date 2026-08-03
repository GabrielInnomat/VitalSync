using System.Collections.Concurrent;
using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Events;

public sealed class ProjectionRunner(IServiceProvider serviceProvider)
{
    private static readonly ConcurrentDictionary<Type, ProjectionInvoker> Invokers = new();

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
