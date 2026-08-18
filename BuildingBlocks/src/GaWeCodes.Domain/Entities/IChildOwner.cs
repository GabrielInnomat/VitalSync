using GaWeCodes.Domain.Events;

namespace GaWeCodes.Domain.Entities;

public interface IChildOwner<TChildKey, TChildState> : IDomainEventRaiser
    where TChildKey : struct, IEntityKey, IEquatable<TChildKey>
    where TChildState : EntityState<TChildState, TChildKey>
{
    TChildState? FindChild(TChildKey childId);
}
