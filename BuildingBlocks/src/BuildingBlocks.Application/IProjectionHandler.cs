using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

public interface IProjectionHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken);
}
