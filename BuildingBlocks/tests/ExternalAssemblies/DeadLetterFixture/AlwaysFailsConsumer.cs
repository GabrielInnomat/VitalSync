using BuildingBlocks.Application;

namespace DeadLetterFixture;

[IntegrationEventTopic("probe.always-fails")]
public sealed record AlwaysFailsIntegrationEvent(string Name) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class AttemptRecorder
{
    private int _attempts;

    public int Attempts => Volatile.Read(ref _attempts);

    public void Record() => Interlocked.Increment(ref _attempts);
}

public sealed class AlwaysFailsConsumer
{
    public static void Handle(AlwaysFailsIntegrationEvent message, AttemptRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(recorder);

        recorder.Record();

        throw new InvalidOperationException($"'{message.Name}' can never be handled.");
    }
}
