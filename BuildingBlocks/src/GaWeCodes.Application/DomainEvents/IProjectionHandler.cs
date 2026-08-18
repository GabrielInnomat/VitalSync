using GaWeCodes.Domain.Events;

namespace GaWeCodes.Application.DomainEvents;

public interface IProjectionHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken);
}
