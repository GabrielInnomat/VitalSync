using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Persistence;
using HullFixture;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class AggregateFactoryTests
{
    [Fact]
    public void CreateEmpty_ReturnsAnUnidentifiedHullThroughThePrivateConstructor()
    {
        var hull = AggregateFactory.CreateEmpty<Counter>();

        Assert.True(hull.Id.IsEmpty);
        Assert.Empty(hull.DomainEvents);
    }

    [Fact]
    public void CreateEmpty_ReturnsADistinctInstanceEachTime()
    {
        var first = AggregateFactory.CreateEmpty<Counter>();
        var second = AggregateFactory.CreateEmpty<Counter>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void EnsureAggregatesAreReconstitutable_WithConformingAggregates_DoesNotThrow()
    {
        AggregateFactory.EnsureAggregatesAreReconstitutable([typeof(FlushProbe).Assembly]);
    }

    [Fact]
    public void EnsureAggregatesAreReconstitutable_WithoutParameterlessConstructor_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => AggregateFactory.EnsureAggregatesAreReconstitutable([typeof(SealedHull).Assembly]));

        Assert.Contains(nameof(SealedHull), exception.Message, StringComparison.Ordinal);
        Assert.Contains("parameterless constructor", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBuildingBlocks_WithAnUnreconstitutableAggregate_FailsAtRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBuildingBlocks(options => options
                .AddDomainEventsFrom(typeof(SealedHull).Assembly)
                .UseEfCorePersistence<FlushProbeContext>("Host=design-time")));

        Assert.Contains(nameof(SealedHull), exception.Message, StringComparison.Ordinal);
    }
}
