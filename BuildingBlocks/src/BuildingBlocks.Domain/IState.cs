namespace BuildingBlocks.Domain;

/// <summary>
/// Immutable state of an event-sourced aggregate. Applying a domain event yields
/// the next state. The self-referencing generic guarantees <see cref="Apply"/>
/// returns the concrete state type, removing the need for a cast at the call site.
/// </summary>
public interface IState<TSelf, out TKey>
    where TSelf : IState<TSelf, TKey>
    where TKey : struct, IEntityKey
{
    TKey Id { get; }

    TSelf Apply(IDomainEvent domainEvent);
}
