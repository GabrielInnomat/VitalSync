namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>
/// Immutable state for the event-sourced aggregate test double. Applying a
/// <see cref="TestDomainEvent"/> / <see cref="RawDomainEvent"/> sets the id and value.
/// </summary>
internal sealed record TestState(TestId Id, int Value) : IState<TestState, TestId>
{
    public static TestState Empty => new(TestId.Empty, 0);

    public TestState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        TestDomainEvent e => this with { Id = new TestId(e.NewValue), Value = e.NewValue },
        RawDomainEvent e => this with { Id = new TestId(e.NewValue), Value = e.NewValue },
        _ => this,
    };
}

/// <summary>State whose <see cref="Apply"/> never sets a non-empty id, to test the identity guard.</summary>
internal sealed record NeverIdentifiedState(TestId Id, int Value) : IState<NeverIdentifiedState, TestId>
{
    public static NeverIdentifiedState Empty => new(TestId.Empty, 0);

    public NeverIdentifiedState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        TestDomainEvent e => this with { Value = e.NewValue },
        _ => this,
    };
}
