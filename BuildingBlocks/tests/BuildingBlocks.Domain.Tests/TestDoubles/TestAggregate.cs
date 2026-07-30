namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>
/// Concrete state-stored aggregate exposing the protected members of
/// <see cref="AggregateRoot{TKey, TState}"/> so tests can drive them.
/// </summary>
internal sealed class TestAggregate(TestId id) : AggregateRoot<TestId, TestState>(new TestState(id, 0))
{
    public TestState CurrentState => State;

    public void Raise(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}

/// <summary>A second aggregate type used for cross-type equality checks.</summary>
internal sealed class OtherTestAggregate(TestId id) : AggregateRoot<TestId, TestState>(new TestState(id, 0));

/// <summary>Aggregate whose applied state never becomes identified, to test the identity guard.</summary>
internal sealed class NeverIdentifiedAggregate()
    : AggregateRoot<TestId, NeverIdentifiedState>(NeverIdentifiedState.Empty)
{
    public void Raise(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}
