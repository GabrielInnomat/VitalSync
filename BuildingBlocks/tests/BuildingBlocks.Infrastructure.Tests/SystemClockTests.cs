using BuildingBlocks.Infrastructure.Time;
using Microsoft.Extensions.Time.Testing;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class SystemClockTests
{
    [Fact]
    public void Now_WithAnOffsetReportingTimeProvider_ReturnsTheSameInstantWithoutOffset()
    {
        var offsetTime = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.FromHours(2));
        var clock = new SystemClock(new FakeTimeProvider(offsetTime));

        var now = clock.Now;

        Assert.Equal(TimeSpan.Zero, now.Offset);
        Assert.Equal(offsetTime, now);
    }

    [Fact]
    public void Now_AfterTimeAdvanced_ReturnsTheAdvancedTime()
    {
        var start = new DateTimeOffset(2026, 7, 29, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(start);
        var clock = new SystemClock(timeProvider);

        timeProvider.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(start.AddMinutes(5), clock.Now);
    }
}
