using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Time;

internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset Now => timeProvider.GetUtcNow().ToUniversalTime();
}
