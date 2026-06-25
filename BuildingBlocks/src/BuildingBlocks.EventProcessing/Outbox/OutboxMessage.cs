namespace BuildingBlocks.EventProcessing.Outbox;

/// <summary>
/// A persisted representation of a domain/integration event awaiting reliable
/// publication (the Transactional Outbox pattern).
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Initializes a new outbox message.</summary>
    /// <param name="id">The unique message id.</param>
    /// <param name="type">The fully qualified event type name.</param>
    /// <param name="content">The serialized event payload.</param>
    /// <param name="occurredOnUtc">When the event occurred (UTC).</param>
    public OutboxMessage(Guid id, string type, string content, DateTimeOffset occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    /// <summary>Gets the unique message id.</summary>
    public Guid Id { get; }

    /// <summary>Gets the fully qualified event type name.</summary>
    public string Type { get; }

    /// <summary>Gets the serialized event payload.</summary>
    public string Content { get; }

    /// <summary>Gets the instant the event occurred (UTC).</summary>
    public DateTimeOffset OccurredOnUtc { get; }

    /// <summary>Gets or sets the instant the message was successfully processed (UTC).</summary>
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    /// <summary>Gets or sets the error captured during a failed processing attempt.</summary>
    public string? Error { get; set; }
}