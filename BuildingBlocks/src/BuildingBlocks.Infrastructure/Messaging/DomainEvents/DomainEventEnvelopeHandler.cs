using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging.DomainEvents;

public sealed class DomainEventEnvelopeHandler(
    IDomainEventPublisher publisher,
    DomainEventEnvelopeSerializer serializer,
    IIntegrationEventSinkFactory sinkFactory)
{
    public Task Handle(DomainEventEnvelope envelope, IMessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var domainEvent = serializer.Unwrap(envelope);
        var metadata = new DomainEventMetadata(
            envelope.EventId,
            envelope.AggregateName,
            envelope.AggregateId,
            envelope.Version,
            envelope.OccurredAt);

        return publisher.PublishAsync(domainEvent, metadata, sinkFactory.Create(context), cancellationToken);
    }
}
