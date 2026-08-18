using System.Diagnostics;
using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Domain.Events;
using GaWeCodes.Telemetry;

namespace GaWeCodes.Messaging.IntegrationEvents;

internal sealed class IntegrationEventPublisher(MapperRunner mapperRunner) : IIntegrationEventPublisher
{
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

    private Task<int> DispatchAsync(
        IDomainEvent domainEvent,
        DomainEventMetadata metadata,
        IIntegrationEventSink integrationEventSink,
        CancellationToken cancellationToken)
        => mapperRunner.RunAsync(domainEvent, metadata, integrationEventSink, cancellationToken);
}
