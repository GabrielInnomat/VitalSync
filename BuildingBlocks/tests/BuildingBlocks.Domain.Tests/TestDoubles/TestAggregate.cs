namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>
/// Concrete aggregate exposing the protected members of
/// <see cref="AggregateRoot{TKey}"/> so tests can drive them.
/// </summary>
public sealed class TestAggregate(TestId id) : AggregateRoot<TestId>(id)
{
    public void RaiseTestEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
}

/// <summary>A second aggregate type used for cross-type equality checks.</summary>
public sealed class OtherTestAggregate(TestId id) : AggregateRoot<TestId>(id);
