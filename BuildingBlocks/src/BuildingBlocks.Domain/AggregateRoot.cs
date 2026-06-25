namespace BuildingBlocks.Domain;

public abstract class AggregateRoot<TKey, TState>(TState initialState)
    : IAggregateRoot<TKey>, IDomainEventsManager, IEquatable<AggregateRoot<TKey, TState>>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TState State { get; private set; } = initialState;

    public TKey Id => State.Id;

    public long Version { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var domainEvent in history)
        {
            State = ApplyToState(domainEvent);
            EnsureValidIdentity();
            Version++;
        }
    }

    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        State = ApplyToState(domainEvent);
        EnsureValidIdentity();
        Version++;
        _domainEvents.Add(domainEvent);
    }

    private TState ApplyToState(IDomainEvent domainEvent)
    {
        return (TState)State.Apply(domainEvent);
    }

    private void EnsureValidIdentity()
    {
        if (State.Id.Value == Guid.Empty)
            throw new DomainValidationException(
                "The aggregate's identity must be set to a non-empty value by the applied event.");
    }

    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public bool Equals(AggregateRoot<TKey, TState>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as AggregateRoot<TKey, TState>);
    }

    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(AggregateRoot<TKey, TState>? left, AggregateRoot<TKey, TState>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(AggregateRoot<TKey, TState>? left, AggregateRoot<TKey, TState>? right)
    {
        return !(left == right);
    }
}