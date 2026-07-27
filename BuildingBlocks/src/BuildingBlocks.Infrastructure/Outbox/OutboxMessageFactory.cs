using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Builds <see cref="OutboxMessage"/>s from the uncommitted domain events of an aggregate.
/// </summary>
/// <remarks>
/// Event-sourced saves supply the base stream position so each message carries the event's real version; state-stored
/// saves have no stream version, so their messages carry position <c>0</c> and the drain loop falls back to the
/// store-generated <see cref="OutboxMessage.Id"/> as the monotonic marker.
/// </remarks>
internal static class OutboxMessageFactory
{
    public static IReadOnlyList<OutboxMessage> CreateMessages(
        string streamId,
        IReadOnlyCollection<IDomainEvent> domainEvents,
        long? basePosition)
    {
        var enqueuedAt = DateTimeOffset.UtcNow;
        var messages = new List<OutboxMessage>(domainEvents.Count);
        var offset = 0;

        foreach (var domainEvent in domainEvents)
        {
            offset++;
            messages.Add(new OutboxMessage
            {
                StreamId = streamId,
                StreamPosition = basePosition.HasValue ? basePosition.Value + offset : 0,
                EventType = OutboxMessageSerializer.GetEventTypeName(domainEvent),
                Payload = OutboxMessageSerializer.Serialize(domainEvent),
                OccurredAt = domainEvent.OccurredAt,
                EnqueuedAt = enqueuedAt,
            });
        }

        return messages;
    }
}
