using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging.DomainEvents;

public sealed class DomainEventEnvelopeHandler(
    IIntegrationEventPublisher publisher,
    DomainEventEnvelopeSerializer serializer,
    IIntegrationEventSinkFactory sinkFactory)
{
    public async Task Handle(DomainEventEnvelope envelope, IMessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(context);

        var domainEvent = serializer.Unwrap(envelope);
        var metadata = DomainEventMetadataFactory.From(envelope);

        await publisher.PublishAsync(domainEvent, metadata, sinkFactory.Create(context), cancellationToken)
            .ConfigureAwait(false);

        await context.PublishAsync(new ProjectionEnvelope(envelope)).ConfigureAwait(false);
    }
}
