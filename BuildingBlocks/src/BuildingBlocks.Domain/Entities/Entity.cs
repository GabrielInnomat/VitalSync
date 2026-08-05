using BuildingBlocks.Domain.Events;
using BuildingBlocks.Domain.Rules;

namespace BuildingBlocks.Domain.Entities;

public abstract class Entity<TKey, TState> : EntityBase<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
    where TState : EntityState<TState, TKey>
{
    private readonly IDomainEventRaiser _raiser;
    private readonly Func<TKey, TState?> _stateLookup;

    protected Entity(IDomainEventRaiser raiser, TKey id, Func<TKey, TState?> stateLookup)
    {
        if (id.IsEmpty)
        {
            throw new DomainValidationException("The id of an entity cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(raiser);
        ArgumentNullException.ThrowIfNull(stateLookup);

        Id = id;
        _raiser = raiser;
        _stateLookup = stateLookup;
    }

    public sealed override TKey Id { get; }

    protected TState GetCurrentState()
    {
        return _stateLookup(Id)
            ?? throw new DomainValidationException(
                $"The entity '{Id}' is no longer part of its aggregate.");
    }

    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _raiser.Raise(domainEvent);
    }
}
