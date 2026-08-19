using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Core.Messaging.DomainEvents;
using GaWeCodes.Core.Messaging.IntegrationEvents;
using GaWeCodes.Wolverine.Messaging.Transport;
using Wolverine;

namespace GaWeCodes.Wolverine.Messaging.DomainEvents;

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
