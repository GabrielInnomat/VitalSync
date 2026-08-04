namespace BuildingBlocks.Domain;

public abstract class AggregateRoot<TKey, TState> : EntityBase<TKey>, IAggregateRoot<TKey>, IDomainEventOwner, IDomainEventRaiser, IStateOwner
    where TKey : struct, IEntityKey
    where TState : AggregateState<TState, TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TState initialState)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        State = initialState;
    }

    protected TState State { get; private set; }

    public sealed override TKey Id => State.Id;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ApplyEvent(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    private protected void ApplyEvent(IDomainEvent domainEvent)
    {
        State = State.Apply(domainEvent).WithVersion(State.Version + 1);

        if (State.Id.IsEmpty)
        {
            throw new DomainValidationException(
                "The aggregate's identity must be set to a non-empty value by the applied event.");
        }
    }

    void IDomainEventOwner.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    void IDomainEventRaiser.Raise(IDomainEvent domainEvent)
    {
        RaiseEvent(domainEvent);
    }

    Type IStateOwner.StateType => typeof(TState);

    object IStateOwner.State => State;

    long IStateOwner.Version => State.Version;

    void IStateOwner.Restore(object state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state is not TState typedState)
        {
            throw new ArgumentException(
                $"The state must be of type '{typeof(TState)}', but was '{state.GetType()}'.",
                nameof(state));
        }

        if (typedState.Id.IsEmpty)
        {
            throw new DomainValidationException(
                "The restored state must carry a non-empty identity.");
        }

        State = typedState;
    }
}
