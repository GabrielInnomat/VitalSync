using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Domain.Events;

namespace GaWeCodes.Application.IntegrationEvents;

public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    IReadOnlyCollection<IIntegrationEvent> Map(TDomainEvent domainEvent, DomainEventMetadata metadata);
}
