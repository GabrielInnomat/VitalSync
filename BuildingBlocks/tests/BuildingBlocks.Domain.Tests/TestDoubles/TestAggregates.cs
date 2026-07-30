namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>
/// Concrete event-sourced aggregate exposing <c>RaiseEvent</c> and the state to the tests.
/// </summary>
internal sealed class TestEventSourcedAggregate()
    : EventSourcedAggregateRoot<TestId, TestState>(TestState.Empty)
{
    public TestState CurrentState => State;

    public void Raise(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}

/// <summary>A second event-sourced aggregate type for cross-type equality checks.</summary>
internal sealed class OtherEventSourcedAggregate()
    : EventSourcedAggregateRoot<TestId, TestState>(TestState.Empty)
{
    public void Raise(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}
