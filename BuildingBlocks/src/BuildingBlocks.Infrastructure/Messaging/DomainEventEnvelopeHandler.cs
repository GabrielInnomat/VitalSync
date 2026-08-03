using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class DomainEventEnvelopeHandler(IDomainEventPublisher publisher, IIntegrationEventSinkFactory sinkFactory)
{
    public Task Handle(DomainEventEnvelope envelope, IMessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var domainEvent = DomainEventEnvelopeSerializer.Unwrap(envelope);
        var metadata = new DomainEventMetadata(envelope.EventId, envelope.OccurredAt);
        return publisher.PublishAsync(domainEvent, metadata, sinkFactory.Create(context), cancellationToken);
    }
}
