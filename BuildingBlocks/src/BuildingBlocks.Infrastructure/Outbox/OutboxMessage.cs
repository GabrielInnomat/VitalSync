namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// A domain event captured durably in the write database, awaiting dispatch by the outbox-backed publisher.
/// </summary>
/// <remarks>
/// Outbox messages are written in the same write-database transaction as the state change (ADR-0022), so an event can
/// never be observed without its state change or vice versa. After commit the drain loop reads pending messages in
/// <see cref="Id"/> order (a monotonic, store-generated sequence), dispatches them, and marks them processed only after
/// all handlers succeed — yielding at-least-once delivery. The type is persistence-shaped by design (mutable
/// properties, store-generated key) so both EF Core and Marten can persist it without extra mapping.
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>
    /// Gets or sets the store-generated, monotonically increasing identifier of the message.
    /// </summary>
    /// <remarks>
    /// The identifier doubles as the global dispatch order and as the fallback stream position for state-stored
    /// aggregates that carry no event-stream version.
    /// </remarks>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the stream (aggregate) that produced the event.
    /// </summary>
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event's position (version) within its aggregate's stream, or <c>0</c> when the aggregate is state-stored and has no stream version.
    /// </summary>
    public long StreamPosition { get; set; }

    /// <summary>
    /// Gets or sets the assembly-qualified CLR type name of the serialized domain event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON payload of the serialized domain event.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp at which the domain event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp at which the message was written to the outbox.
    /// </summary>
    public DateTimeOffset EnqueuedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of failed dispatch attempts so far.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the earliest timestamp at which the next dispatch attempt may run, or <see langword="null"/> when the message may be dispatched immediately.
    /// </summary>
    /// <remarks>
    /// Set by the drain loop after a failed attempt to apply an exponential retry backoff.
    /// </remarks>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp at which the message was successfully dispatched, or <see langword="null"/> while it is still pending.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }
}
