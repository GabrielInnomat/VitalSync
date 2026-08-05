using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed class TestAggregate(TestId id) : AggregateRoot<TestId, TestState>(new TestState(id, 0))
{
    public TestState CurrentState => State;

    public void Raise(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}

internal sealed class OtherTestAggregate(TestId id) : AggregateRoot<TestId, TestState>(new TestState(id, 0));

internal sealed class NeverIdentifiedAggregate()
    : AggregateRoot<TestId, NeverIdentifiedState>(NeverIdentifiedState.Empty)
{
    public void Raise(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}
