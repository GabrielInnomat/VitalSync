namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed class ReconstitutableAggregate
    : AggregateRoot<TestId, TestState>
{
    private ReconstitutableAggregate() : base(TestState.Empty)
    {
    }

    public TestState CurrentState => State;

    public static ReconstitutableAggregate Create(int value)
    {
        var aggregate = new ReconstitutableAggregate();
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
