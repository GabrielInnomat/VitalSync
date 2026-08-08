using System.Collections.Concurrent;
using System.Diagnostics;
using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Messaging.DomainEvents;

internal sealed class ProjectionRunner(IServiceProvider serviceProvider)
{
    private static readonly ConcurrentDictionary<Type, ProjectionInvoker> Invokers = new();

    public Task RunAsync(IDomainEvent domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

        var invoker = Invokers.GetOrAdd(
            domainEvent.GetType(),
            static type => (ProjectionInvoker)Activator.CreateInstance(
                typeof(ProjectionInvoker<>).MakeGenericType(type))!);

        return invoker.InvokeAsync(domainEvent, metadata, serviceProvider, cancellationToken);
    }

    private abstract class ProjectionInvoker
    {
        public abstract Task InvokeAsync(
            IDomainEvent domainEvent,
            DomainEventMetadata metadata,
            IServiceProvider services,
            CancellationToken cancellationToken);
    }

    private sealed class ProjectionInvoker<TDomainEvent> : ProjectionInvoker
        where TDomainEvent : IDomainEvent
    {
        public override async Task InvokeAsync(
            IDomainEvent domainEvent,
            DomainEventMetadata metadata,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var typedEvent = (TDomainEvent)domainEvent;
            foreach (var handler in services.GetServices<IProjectionHandler<TDomainEvent>>())
            {
                await InvokeHandlerAsync(handler, typedEvent, metadata, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task InvokeHandlerAsync(
            IProjectionHandler<TDomainEvent> handler,
            TDomainEvent domainEvent,
            DomainEventMetadata metadata,
            CancellationToken cancellationToken)
        {
            if (!BuildingBlocksTelemetry.Source.HasListeners())
            {
                await handler.HandleAsync(domainEvent, metadata, cancellationToken).ConfigureAwait(false);
                return;
            }

            var handlerName = handler.GetType().Name;
            using var activity = BuildingBlocksTelemetry.Source.StartActivity(
                $"Project {handlerName}",
                ActivityKind.Internal);

            activity?.SetTag(TelemetryTags.ProjectionHandler, handler.GetType().FullName);
            activity?.SetTag(TelemetryTags.DomainEventName, typeof(TDomainEvent).Name);
            activity?.SetTag(TelemetryTags.AggregateName, metadata.AggregateName);
            activity?.SetTag(TelemetryTags.AggregateId, metadata.AggregateId);
            activity?.SetTag(TelemetryTags.AggregateVersion, metadata.Version);

            try
            {
                await handler.HandleAsync(domainEvent, metadata, cancellationToken).ConfigureAwait(false);
                activity?.MarkSucceeded();
            }
            catch (Exception exception)
            {
                activity?.MarkFaulted(exception);
                throw;
            }
        }
    }
}
