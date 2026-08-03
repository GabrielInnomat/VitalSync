namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>
/// A state-stored aggregate written the way the reconstitution amendment of ADR-0025 prescribes: private
/// parameterless constructor, <see cref="IReconstitutable{TSelf}"/> implemented explicitly, and a named factory
/// as the only public way in.
/// </summary>
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

/// <summary>
/// The event-sourced counterpart of <see cref="ReconstitutableAggregate"/>, used to show that reconstitution is
/// written identically on both persistence paths.
/// </summary>
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
