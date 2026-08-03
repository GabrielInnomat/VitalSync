namespace BuildingBlocks.Domain;

public abstract class AggregateRoot<TKey, TState> : EntityBase<TKey>, IAggregateRoot<TKey>, IDomainEventOwner, IStateOwner
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
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
        OnEventRaised();
    }

    private protected void ApplyEvent(IDomainEvent domainEvent)
    {
        State = State.Apply(domainEvent);

        if (State.Id.IsEmpty)
        {
            throw new DomainValidationException(
                "The aggregate's identity must be set to a non-empty value by the applied event.");
        }
    }

    private protected virtual void OnEventRaised()
    {
    }

    void IDomainEventOwner.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    Type IStateOwner.StateType => typeof(TState);

    object IStateOwner.State => State;

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
