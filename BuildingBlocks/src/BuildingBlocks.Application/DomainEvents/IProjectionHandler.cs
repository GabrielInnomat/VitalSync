using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Application.DomainEvents;

public interface IProjectionHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken);
}
