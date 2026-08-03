namespace BuildingBlocks.Infrastructure.Messaging;

public sealed record DomainEventEnvelope(
    string EventName,
    string Payload,
    Guid EventId,
    string AggregateName,
    string AggregateId,
    long Version,
    DateTimeOffset OccurredAt);
