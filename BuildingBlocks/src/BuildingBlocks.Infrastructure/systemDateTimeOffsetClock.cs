using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure;

internal sealed class systemDateTimeOffsetClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
