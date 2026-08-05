using BuildingBlocks.Application.IntegrationEvents;

namespace DeadLetterFixture;

[IntegrationEventTopic("upstream.always-fails")]
public sealed record AlwaysFailsIntegrationEvent(string Name) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class AttemptRecorder
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _names = new();

    public int Attempts => _names.Count;

    public IReadOnlyCollection<string> Names => [.. _names];

    public void Record(string name) => _names.Enqueue(name);
}

public sealed class AlwaysFailsConsumer
{
    public static void Handle(AlwaysFailsIntegrationEvent message, AttemptRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(recorder);

        recorder.Record(message.Name);

        throw new InvalidOperationException($"'{message.Name}' can never be handled.");
    }
}
