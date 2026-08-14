using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Domain.Entities;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Domain.Naming;

namespace BuildingBlocks.Infrastructure.Tests;
public readonly record struct CounterId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;
}

[EventName("counter-created-v1")]
public sealed record CounterCreated(CounterId CounterId) : DomainEvent;

[EventName("counter-incremented-v1")]
public sealed record CounterIncremented(CounterId CounterId, int By) : DomainEvent;

public sealed record CounterState(CounterId Id, int Total) : AggregateState<CounterState, CounterId>
{
    public static CounterState Empty => new(new CounterId(Guid.Empty), 0);

    public override CounterState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        CounterCreated created => this with { Id = created.CounterId },
        CounterIncremented incremented => this with { Id = incremented.CounterId, Total = Total + incremented.By },
        _ => this,
    };
}

[AggregateName("counter")]
public sealed class Counter : EventSourcedAggregateRoot<CounterId, CounterState>
{
    private Counter() : base(CounterState.Empty)
    {
    }

    public int Total => State.Total;

    public static Counter Create(CounterId id)
    {
        var counter = new Counter();
        counter.RaiseEvent(new CounterCreated(id));
        return counter;
    }

    public void Increment(int by) => RaiseEvent(new CounterIncremented(Id, by));
}
