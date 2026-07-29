using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Events;

internal static class DomainEventStamper
{
    public static IDomainEvent Stamp(IDomainEvent domainEvent, DateTimeOffset occurredAt) =>
        domainEvent is DomainEvent { OccurredAt.Ticks: 0 } record
            ? record with { OccurredAt = occurredAt }
            : domainEvent;
}
