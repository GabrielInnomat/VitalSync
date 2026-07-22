using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

public sealed class AggregateRootTests
{
    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainValidationException()
    {
        var ex = Assert.Throws<DomainValidationException>(() => new TestAggregate(TestId.Empty));
        Assert.Equal("The id of an aggregate cannot be empty.", ex.Message);
    }

    [Fact]
    public void Constructor_WithValidId_SetsId()
    {
        var id = new TestId(7);

        var aggregate = new TestAggregate(id);

        Assert.Equal(id, aggregate.Id);
    }

    [Fact]
    public void NewAggregate_HasNoDomainEvents()
    {
        var aggregate = new TestAggregate(new TestId(1));

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_SurfacesEventInOrder()
    {
        var aggregate = new TestAggregate(new TestId(1));
        var first = new TestDomainEvent(1);
        var second = new TestDomainEvent(2);

        aggregate.RaiseTestEvent(first);
        aggregate.RaiseTestEvent(second);

        Assert.Equal(new IDomainEvent[] { first, second }, aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheCollection()
    {
        var aggregate = new TestAggregate(new TestId(1));
        aggregate.RaiseTestEvent(new TestDomainEvent(1));

        ((IDomainEventsManager)aggregate).ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Equals_SameTypeSameId_AreEqual()
    {
        var a = new TestAggregate(new TestId(1));
        var b = new TestAggregate(new TestId(1));

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_SameIdDifferentType_AreNotEqual()
    {
        var a = new TestAggregate(new TestId(1));
        var b = new OtherTestAggregate(new TestId(1));

        Assert.False(a.Equals(b as object));
    }

    [Fact]
    public void Equals_DifferentId_AreNotEqual()
    {
        var a = new TestAggregate(new TestId(1));
        var b = new TestAggregate(new TestId(2));

        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void EqualityOperator_BothNull_AreEqual()
    {
        TestAggregate? a = null;
        TestAggregate? b = null;

        Assert.True(a == b);
    }

    [Fact]
    public void EqualityOperator_OneNull_AreNotEqual()
    {
        var a = new TestAggregate(new TestId(1));

        Assert.False(a == null);
        Assert.False(null == a);
        Assert.True(a != null);
    }
}
