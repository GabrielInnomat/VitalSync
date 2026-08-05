using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Infrastructure.Messaging.DomainEvents;

internal sealed class DomainEventPublisher(
    ProjectionRunner projectionRunner,
    IEnumerable<IIntegrationEventMapper> mappers) : IDomainEventPublisher
{
    private readonly IIntegrationEventMapper[] _mappers = [.. mappers];

    public async Task PublishAsync(IDomainEvent domainEvent, DomainEventMetadata metadata, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(integrationEventSink);

        await projectionRunner.RunAsync(domainEvent, metadata, cancellationToken).ConfigureAwait(false);

        foreach (var mapper in _mappers)
        {
            foreach (var integrationEvent in mapper.Map(domainEvent, metadata))
            {
                await integrationEventSink.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
