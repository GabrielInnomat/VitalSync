namespace BuildingBlocks.Domain;

public abstract class EventSourcedAggregateRoot<TKey, TState>(TState initialState)
    : AggregateRoot<TKey, TState>(initialState), IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : AggregateState<TState, TKey>
{
    void IEventSourcedAggregateRoot<TKey>.LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);

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
