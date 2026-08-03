namespace BuildingBlocks.Application;

public sealed record DomainEventMetadata(
    Guid EventId,
    string AggregateName,
    string AggregateId,
    long Version,
    DateTimeOffset OccurredAt);
