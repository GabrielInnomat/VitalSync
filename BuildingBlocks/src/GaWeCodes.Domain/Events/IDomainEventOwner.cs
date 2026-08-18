namespace GaWeCodes.Domain.Events;

public interface IDomainEventOwner : IHasDomainEvents
{
    void ClearDomainEvents();
}
