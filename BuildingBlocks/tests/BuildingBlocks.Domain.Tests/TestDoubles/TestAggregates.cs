namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>
/// Concrete event-sourced aggregate exposing <c>RaiseEvent</c> to the tests.
/// </summary>
internal sealed class TestEventSourcedAggregate
    : EventSourcedAggregateRoot<TestId, TestState>
{
    public TestEventSourcedAggregate() : base(TestState.Empty)
    {
    }

    public void Raise(IDomainEvent domainEvent, IClock clock) => RaiseEvent(domainEvent, clock);
}

/// <summary>A second event-sourced aggregate type for cross-type equality checks.</summary>
internal sealed class OtherEventSourcedAggregate
    : EventSourcedAggregateRoot<TestId, TestState>
{
    public OtherEventSourcedAggregate() : base(TestState.Empty)
    {
    }

    public void Raise(IDomainEvent domainEvent, IClock clock) => RaiseEvent(domainEvent, clock);
}

/// <summary>Aggregate whose applied state never becomes identified, to test the guard.</summary>
internal sealed class NeverIdentifiedAggregate
    : EventSourcedAggregateRoot<TestId, NeverIdentifiedState>
{
    public NeverIdentifiedAggregate() : base(NeverIdentifiedState.Empty)
    {
    }

    public void Raise(IDomainEvent domainEvent, IClock clock) => RaiseEvent(domainEvent, clock);
}
