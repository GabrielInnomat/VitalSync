using BuildingBlocks.Domain;

namespace BuildingBlocks.Domain.Tests;

// A minimal aggregate + state + events used to exercise AggregateRoot<TKey, TState>.

public readonly record struct TestId(Guid Value) : IEntityKey;

public sealed record TestState : IState<TestId>
{
    public TestId Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public static TestState Empty => new();

    public IState<TestId> Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        TestCreated created => this with { Id = created.Id, Name = created.Name },
        TestRenamed renamed => this with { Name = renamed.NewName },
        _ => this
    };
}

public sealed record TestCreated(TestId Id, string Name) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record TestRenamed(string NewName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class TestAggregate : AggregateRoot<TestId, TestState>
{
    public TestAggregate() : base(TestState.Empty) { }

    public static TestAggregate Create(TestId id, string name)
    {
        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestCreated(id, name));
        return aggregate;
    }

    public void Rename(string newName) => RaiseEvent(new TestRenamed(newName));

    // Expose a way to raise an arbitrary event for guard tests.
    public void RaiseUnchecked(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}
