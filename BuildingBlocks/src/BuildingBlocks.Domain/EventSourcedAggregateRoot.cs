namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for event-sourced aggregate roots, whose event history is itself business value.
/// </summary>
/// <remarks>
/// Event sourcing is a purely additive capability on top of <see cref="AggregateRoot{TKey, TState}"/>: this class
/// contributes only a version for optimistic concurrency and the ability to rebuild state by replaying history.
/// Because the authoring model (state fold via <c>RaiseEvent</c>) is shared with the base, moving an aggregate
/// between the state-stored and event-sourced worlds is a change of base class and repository registration — the
/// business logic stays untouched. Both members are implemented explicitly, so they are reachable only through the
/// <see cref="IEventSourcedAggregateRoot{TKey}"/> view used by infrastructure.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
/// <typeparam name="TState">The type of the aggregate root's state.</typeparam>
/// <param name="initialState">The initial state of the aggregate root.</param>
public abstract class EventSourcedAggregateRoot<TKey, TState>(TState initialState)
    : AggregateRoot<TKey, TState>(initialState), IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
{
    private long _version;

    /// <inheritdoc/>
    long IEventSourcedAggregateRoot<TKey>.Version => _version;

    /// <inheritdoc/>
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
            _version++;
        }
    }

    /// <summary>
    /// Advances the aggregate's version after an event has been raised.
    /// </summary>
    private protected sealed override void OnEventRaised()
    {
        _version++;
    }
}
