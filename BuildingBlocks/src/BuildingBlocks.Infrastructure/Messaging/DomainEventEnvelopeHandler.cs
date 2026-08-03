using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class DomainEventEnvelopeHandler(IDomainEventPublisher publisher, IIntegrationEventSinkFactory sinkFactory)
{
    public Task Handle(DomainEventEnvelope envelope, IMessageContext context, CancellationToken cancellationToken)
    {
        var domainEvent = DomainEventEnvelopeSerializer.Unwrap(envelope);
        return publisher.PublishAsync(domainEvent, sinkFactory.Create(context), cancellationToken);
    }
}
