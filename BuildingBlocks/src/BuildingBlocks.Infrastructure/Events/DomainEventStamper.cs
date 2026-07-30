using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Events;

/// <summary>
/// Stamps a domain event with the transaction's commit time when it has not been stamped yet.
/// </summary>
/// <remarks>
/// <see cref="Domain.DomainEvent.OccurredAt"/> is left unset by the domain (<c>RaiseEvent</c> records pure facts, not
/// time); the domain-correct instant is the commit time, which is shared by every event of a transaction and known
/// only at the unit-of-work boundary. Both unit-of-work implementations call this with a single
/// <see cref="IClock.Now"/> value so state-stored and event-sourced events are stamped identically. An event whose
/// timestamp is already set (for example when replayed) is returned unchanged, making stamping idempotent.
/// </remarks>
internal static class DomainEventStamper
{
    public static IDomainEvent Stamp(IDomainEvent domainEvent, DateTimeOffset occurredAt) =>
        domainEvent is DomainEvent { OccurredAt.Ticks: 0 } record
            ? record with { OccurredAt = occurredAt }
            : domainEvent;
}
