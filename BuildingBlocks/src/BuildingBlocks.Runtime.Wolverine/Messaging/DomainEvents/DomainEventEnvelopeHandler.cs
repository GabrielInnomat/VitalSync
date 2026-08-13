using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging.Transport;
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
        var emitter = new WolverineMessageEmitter(context);

        await publisher.PublishAsync(domainEvent, metadata, sinkFactory.Create(emitter), cancellationToken)
            .ConfigureAwait(false);

        await emitter.PublishAsync(new ProjectionEnvelope(envelope), null, cancellationToken).ConfigureAwait(false);
    }
}
