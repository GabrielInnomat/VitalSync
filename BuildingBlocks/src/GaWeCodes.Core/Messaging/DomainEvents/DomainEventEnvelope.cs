using GaWeCodes.Domain.Naming;

namespace GaWeCodes.Core.Messaging.DomainEvents;

public sealed record DomainEventEnvelope(
    string EventName,
    string Payload,
    Guid EventId,
    string AggregateName,
    string AggregateId,
    long Version,
    DateTimeOffset OccurredAt);
