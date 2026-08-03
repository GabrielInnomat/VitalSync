namespace BuildingBlocks.Domain;

public abstract class EventSourcedAggregateRoot<TKey, TState>(TState initialState)
    : AggregateRoot<TKey, TState>(initialState), IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : AggregateState<TState, TKey>
{
    long IEventSourcedAggregateRoot<TKey>.Version => State.Version;

    void IEventSourcedAggregateRoot<TKey>.LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (DomainEvents.Count > 0)
        {
            throw new InvalidOperationException(
                "LoadFromHistory cannot be called after events have been raised on the aggregate.");
        }

        foreach (var domainEvent in history)
        {
            ApplyEvent(domainEvent);
        }
    }
}
