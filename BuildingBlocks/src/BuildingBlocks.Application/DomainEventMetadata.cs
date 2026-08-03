namespace BuildingBlocks.Application;

public sealed record DomainEventMetadata(Guid EventId, DateTimeOffset OccurredAt);
