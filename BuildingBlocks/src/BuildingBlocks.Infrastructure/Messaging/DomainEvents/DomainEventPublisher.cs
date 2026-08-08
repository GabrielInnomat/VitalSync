using System.Diagnostics;
using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Infrastructure.Telemetry;

namespace BuildingBlocks.Infrastructure.Messaging.DomainEvents;

internal sealed class DomainEventPublisher(
    ProjectionRunner projectionRunner,
    IEnumerable<IIntegrationEventMapper> mappers) : IDomainEventPublisher
{
    private readonly IIntegrationEventMapper[] _mappers = [.. mappers];

    public async Task PublishAsync(IDomainEvent domainEvent, DomainEventMetadata metadata, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(integrationEventSink);

        if (!BuildingBlocksTelemetry.Source.HasListeners())
        {
            await DispatchAsync(domainEvent, metadata, integrationEventSink, cancellationToken).ConfigureAwait(false);
            return;
        }

        var domainEventName = domainEvent.GetType().Name;
        using var activity = BuildingBlocksTelemetry.Source.StartActivity(
            $"Publish {domainEventName}",
            ActivityKind.Internal);

        activity?.SetTag(TelemetryTags.DomainEventName, domainEventName);
        activity?.SetTag(TelemetryTags.AggregateName, metadata.AggregateName);
        activity?.SetTag(TelemetryTags.AggregateId, metadata.AggregateId);
        activity?.SetTag(TelemetryTags.AggregateVersion, metadata.Version);

        try
        {
            var published = await DispatchAsync(domainEvent, metadata, integrationEventSink, cancellationToken)
                .ConfigureAwait(false);

            activity?.SetTag(TelemetryTags.IntegrationEventsPublished, published);
            activity?.MarkSucceeded();
        }
        catch (Exception exception)
        {
            activity?.MarkFaulted(exception);
            throw;
        }
    }

    private async Task<int> DispatchAsync(
        IDomainEvent domainEvent,
        DomainEventMetadata metadata,
        IIntegrationEventSink integrationEventSink,
        CancellationToken cancellationToken)
    {
        await projectionRunner.RunAsync(domainEvent, metadata, cancellationToken).ConfigureAwait(false);

        var published = 0;
        foreach (var mapper in _mappers)
        {
            foreach (var integrationEvent in mapper.Map(domainEvent, metadata))
            {
                await integrationEventSink.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                published++;
            }
        }

        return published;
    }
}
