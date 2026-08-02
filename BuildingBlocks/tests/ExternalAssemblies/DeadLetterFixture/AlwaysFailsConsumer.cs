using BuildingBlocks.Application;
using Wolverine.Attributes;

namespace DeadLetterFixture;

// Lives outside the test assembly on purpose. SubscribeToIntegrationEvents takes the assembly holding a
// service's consumers, and passing the test assembly would drag every other *Handler fixture in it into
// Wolverine's conventional discovery. This mirrors how a real service points at its Infrastructure assembly.

[Topic("probe.always-fails")]
public sealed record AlwaysFailsIntegrationEvent(string Name) : IIntegrationEvent;

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

        // A consumer that can never succeed - a poison message. The point of the retry and dead-letter policy
        // is that this neither blocks the queue forever nor disappears.
        throw new InvalidOperationException($"'{message.Name}' can never be handled.");
    }
}
