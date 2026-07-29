using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Time;

/// <summary>
/// Default <see cref="IClock"/> implementation that reads the current UTC time from a <see cref="TimeProvider"/>.
/// </summary>
/// <remarks>
/// This adapter keeps <see cref="TimeProvider"/> — a broad infrastructure abstraction covering timers and time zones —
/// out of the domain, which only ever needs the narrow "what is now" port. Everything that is persisted, projected or
/// transported across a service boundary is UTC; time zones are a presentation concern and belong in the frontend.
/// The returned timestamp is normalized to a zero offset, so the UTC promise holds even for a time provider that
/// reports the current instant with an offset of its own. Tests substitute a <c>FakeTimeProvider</c> for the registered
/// <see cref="TimeProvider"/> rather than writing their own clock.
/// </remarks>
/// <param name="timeProvider">The time provider the current timestamp is read from.</param>
internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    /// <inheritdoc/>
    public DateTimeOffset Now => timeProvider.GetUtcNow().ToUniversalTime();
}
