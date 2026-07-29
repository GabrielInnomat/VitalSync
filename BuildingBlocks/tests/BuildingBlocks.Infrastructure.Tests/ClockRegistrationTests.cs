using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class ClockRegistrationTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 7, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddBuildingBlocks_RegistersAClock()
    {
        using var provider = new ServiceCollection()
            .AddBuildingBlocks(_ => { })
            .BuildServiceProvider();

        Assert.NotNull(provider.GetService<IClock>());
    }

    [Fact]
    public void AddBuildingBlocks_RegistersAClockBackedByTheRegisteredTimeProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(FixedInstant));

        using var provider = services
            .AddBuildingBlocks(_ => { })
            .BuildServiceProvider();

        Assert.Equal(FixedInstant, provider.GetRequiredService<IClock>().Now);
    }

    [Fact]
    public void AddBuildingBlocks_WithAnAlreadyRegisteredClock_KeepsThatClock()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(new StoppedClock(FixedInstant));

        using var provider = services
            .AddBuildingBlocks(_ => { })
            .BuildServiceProvider();

        Assert.IsType<StoppedClock>(provider.GetRequiredService<IClock>());
    }

    private sealed class StoppedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }
}
