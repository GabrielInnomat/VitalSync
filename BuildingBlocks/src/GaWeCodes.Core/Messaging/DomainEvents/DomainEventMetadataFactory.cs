using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Domain.Naming;

namespace GaWeCodes.Core.Messaging.DomainEvents;

public static class DomainEventMetadataFactory
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
