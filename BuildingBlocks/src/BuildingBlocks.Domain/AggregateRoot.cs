namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for all aggregate roots, whose state changes are expressed by applying domain events to a state object.
/// </summary>
/// <remarks>
/// This is the single authoring model for aggregates (unified per the superseding of ADR-0012): every state change
/// goes through <see cref="RaiseEvent"/>, which folds the event into the immutable <see cref="State"/>
/// (see ADR-0010), validates the identity, and records the event for dispatch after the aggregate has been
/// persisted. Whether the aggregate is stored as current state (EF Core) or as an event stream is a persistence
/// decision made in the composition layer, not a class-hierarchy decision; deriving from
/// <see cref="EventSourcedAggregateRoot{TKey, TState}"/> merely adds the replay/versioning capability on top of this
/// base. Two aggregate roots are considered equal when they are the same concrete type and share the same
/// <see cref="Id"/>.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
/// <typeparam name="TState">The type of the aggregate root's state.</typeparam>
public abstract class AggregateRoot<TKey, TState> : EntityBase<TKey>, IAggregateRoot<TKey>, IDomainEventsManager
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TKey, TState}"/> class with the specified initial state.
    /// </summary>
    /// <remarks>
    /// A brand-new aggregate starts from an empty state and obtains its identity from the first applied event; a
    /// rehydrated aggregate starts from the persisted state. The identity is therefore validated at every state
    /// transition rather than at construction.
    /// </remarks>
    /// <param name="initialState">The initial state of the aggregate root.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="initialState"/> is <see langword="null"/>.</exception>
    protected AggregateRoot(TState initialState)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        State = initialState;
    }

    /// <summary>
    /// Gets the current state of the aggregate root.
    /// </summary>
    /// <remarks>
    /// The state, represented by an instance of <typeparamref name="TState"/>, encapsulates the aggregate's data and
    /// is replaced by a new instance whenever a domain event is applied.
    /// </remarks>
    protected TState State { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the aggregate root.
    /// </summary>
    /// <remarks>
    /// The identifier, of type <typeparamref name="TKey"/>, is derived from the current <see cref="State"/>.
    /// </remarks>
    public sealed override TKey Id => State.Id;

    /// <inheritdoc/>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event, applies it to the aggregate's state, and records it for dispatch.
    /// </summary>
    /// <remarks>
    /// Call this from within the aggregate whenever a state change occurs: the event is folded into
    /// <see cref="State"/>, the resulting identity is validated, and the event is recorded so it can be dispatched
    /// after the aggregate has been persisted. This is the only way to change the aggregate's state.
    /// </remarks>
    /// <param name="domainEvent">The domain event to raise and apply to the aggregate's state.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainValidationException">Thrown when the applied event leaves the aggregate without a usable identity.</exception>
    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ApplyEvent(domainEvent);
        _domainEvents.Add(domainEvent);
        OnEventRaised();
    }

    /// <summary>
    /// Applies a domain event to the aggregate's state without recording it as uncommitted.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="RaiseEvent"/> for new events and by replay
    /// (<see cref="EventSourcedAggregateRoot{TKey, TState}"/>) for historical events, so both paths share the same
    /// fold-and-validate logic.
    /// </remarks>
    /// <param name="domainEvent">The domain event to apply to the aggregate's state.</param>
    /// <exception cref="DomainValidationException">Thrown when the applied event leaves the aggregate without a usable identity.</exception>
    private protected void ApplyEvent(IDomainEvent domainEvent)
    {
        State = State.Apply(domainEvent);

        if (State.Id.IsEmpty)
        {
            throw new DomainValidationException(
                "The aggregate's identity must be set to a non-empty value by the applied event.");
        }
    }

    /// <summary>
    /// Called after <see cref="RaiseEvent"/> has applied and recorded a domain event.
    /// </summary>
    /// <remarks>
    /// Extension point for the in-package event-sourced base to advance its version; it carries no behavior here.
    /// </remarks>
    private protected virtual void OnEventRaised()
    {
    }

    /// <inheritdoc/>
    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
