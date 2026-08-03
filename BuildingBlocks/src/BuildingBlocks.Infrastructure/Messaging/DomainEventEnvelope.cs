namespace BuildingBlocks.Infrastructure.Messaging;

public sealed record DomainEventEnvelope(string EventTypeName, string Payload, Guid EventId, DateTimeOffset OccurredAt);
