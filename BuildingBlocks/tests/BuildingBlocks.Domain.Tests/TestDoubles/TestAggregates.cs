using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed class TestEventSourcedAggregate()
    : EventSourcedAggregateRoot<TestId, TestState>(TestState.Empty)
{
    public TestState CurrentState => State;

    public void Raise(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}

internal sealed class OtherEventSourcedAggregate()
    : EventSourcedAggregateRoot<TestId, TestState>(TestState.Empty)
{
    public void Raise(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}
