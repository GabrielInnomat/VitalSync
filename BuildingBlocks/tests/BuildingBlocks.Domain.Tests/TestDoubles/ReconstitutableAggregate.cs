namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed class ReconstitutableAggregate
    : AggregateRoot<TestId, TestState>, IReconstitutable<ReconstitutableAggregate>
{
    private ReconstitutableAggregate() : base(TestState.Empty)
    {
    }

    public TestState CurrentState => State;

    static ReconstitutableAggregate IReconstitutable<ReconstitutableAggregate>.CreateEmpty() => new();

    public static ReconstitutableAggregate Create(int value)
    {
        var aggregate = new ReconstitutableAggregate();
        aggregate.RaiseEvent(new TestDomainEvent(value));
        return aggregate;
    }
}

internal sealed class ReconstitutableEventSourcedAggregate
    : EventSourcedAggregateRoot<TestId, TestState>, IReconstitutable<ReconstitutableEventSourcedAggregate>
{
    private ReconstitutableEventSourcedAggregate() : base(TestState.Empty)
    {
    }

    static ReconstitutableEventSourcedAggregate IReconstitutable<ReconstitutableEventSourcedAggregate>.CreateEmpty() =>
        new();

    public static ReconstitutableEventSourcedAggregate Create(int value)
    {
        var aggregate = new ReconstitutableEventSourcedAggregate();
        aggregate.RaiseEvent(new TestDomainEvent(value));
        return aggregate;
    }
}
