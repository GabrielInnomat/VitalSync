namespace GaWeCodes.Domain.Events;

public interface IDomainEventRaiser
{
    void Raise(IDomainEvent domainEvent);
}
