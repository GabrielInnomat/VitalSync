using BuildingBlocks.Application.DomainEvents;

namespace BuildingBlocks.Infrastructure.Messaging.DomainEvents;

internal static class DomainEventMetadataFactory
{
    public static DomainEventMetadata From(DomainEventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return new DomainEventMetadata(
            envelope.EventId,
            envelope.AggregateName,
            envelope.AggregateId,
            envelope.Version,
            envelope.OccurredAt);
    }
}
