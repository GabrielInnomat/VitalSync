using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

public sealed class EntityTests
{
    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainValidationException()
    {
        var ex = Assert.Throws<DomainValidationException>(() => new TestEntity(TestId.Empty));
        Assert.Equal("The id of an entity cannot be empty.", ex.Message);
    }

    [Fact]
    public void Constructor_WithValidId_SetsId()
    {
        var id = new TestId(42);

        var entity = new TestEntity(id);

        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void Equals_SameTypeSameId_AreEqual()
    {
        var a = new TestEntity(new TestId(1));
        var b = new TestEntity(new TestId(1));

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_SameIdDifferentType_AreNotEqual()
    {
        var a = new TestEntity(new TestId(1));
        var b = new OtherTestEntity(new TestId(1));

        Assert.False(a.Equals(b as object));
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentId_AreNotEqual()
    {
        var a = new TestEntity(new TestId(1));
        var b = new TestEntity(new TestId(2));

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var a = new TestEntity(new TestId(1));

        Assert.False(a.Equals(null));
        Assert.False(a.Equals((object?)null));
    }

    [Fact]
    public void Equals_WithNonEntityObject_ReturnsFalse()
    {
        var a = new TestEntity(new TestId(1));

        Assert.False(a.Equals("not an entity"));
    }

    [Fact]
    public void EqualityOperator_BothNull_AreEqual()
    {
        TestEntity? a = null;
        TestEntity? b = null;

        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void EqualityOperator_OneNull_AreNotEqual()
    {
        var a = new TestEntity(new TestId(1));
        TestEntity? b = null;

        Assert.False(a == b);
        Assert.False(b == a);
        Assert.True(a != b);
    }

    [Fact]
    public void EqualityOperator_SameReference_AreEqual()
    {
        var a = new TestEntity(new TestId(1));
        var b = a;

        Assert.True(a == b);
    }
}
