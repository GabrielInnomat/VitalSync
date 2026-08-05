using BuildingBlocks.Domain.Aggregates;

namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed class ReconstitutedAggregate
    : AggregateRoot<TestId, TestState>
{
    private ReconstitutedAggregate() : base(TestState.Empty)
    {
    }

    public TestState CurrentState => State;

    public static ReconstitutedAggregate Create(int value)
    {
        var aggregate = new ReconstitutedAggregate();
        aggregate.RaiseEvent(new TestDomainEvent(value));
        return aggregate;
    }
}

internal sealed class ReconstitutableEventSourcedAggregate
    : EventSourcedAggregateRoot<TestId, TestState>
{
    private ReconstitutableEventSourcedAggregate() : base(TestState.Empty)
    {
    }

    public static ReconstitutableEventSourcedAggregate Create(int value)
    {
        var aggregate = new ReconstitutableEventSourcedAggregate();
        aggregate.RaiseEvent(new TestDomainEvent(value));
        return aggregate;
    }
}
