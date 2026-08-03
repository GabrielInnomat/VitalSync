namespace BuildingBlocks.Domain.Tests.TestDoubles;

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

internal sealed record NeverIdentifiedState(TestId Id, int Value) : IState<NeverIdentifiedState, TestId>
{
    public static NeverIdentifiedState Empty => new(TestId.Empty, 0);

    public NeverIdentifiedState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        TestDomainEvent e => this with { Value = e.NewValue },
        _ => this,
    };
}
