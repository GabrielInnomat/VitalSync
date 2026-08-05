using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

public sealed class DomainEventTests
{
    [Fact]
    public void Records_WithSameData_AreValueEqual()
    {
        var first = new TestDomainEvent(1);
        var second = new TestDomainEvent(1);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Records_WithDifferentData_AreNotEqual()
    {
        var first = new TestDomainEvent(1);
        var second = new TestDomainEvent(2);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Records_WithSameData_HaveSameHashCode()
    {
        var first = new TestDomainEvent(1);
        var second = new TestDomainEvent(1);

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
