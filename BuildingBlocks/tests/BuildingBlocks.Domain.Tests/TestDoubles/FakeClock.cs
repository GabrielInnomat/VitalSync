namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>
/// Deterministic <see cref="IClock"/> returning a fixed timestamp.
/// </summary>
internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; } = now;
}
