using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;

namespace BuildingBlocks.Infrastructure.Events;

internal sealed class Publisher(
    ProjectionRunner projectionRunner,
    IEnumerable<IIntegrationEventMapper> mappers) : IDomainEventPublisher
{
    private readonly IIntegrationEventMapper[] _mappers = [.. mappers];

    public async Task PublishAsync(IDomainEvent domainEvent, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(integrationEventSink);

        await projectionRunner.RunAsync(domainEvent, cancellationToken).ConfigureAwait(false);

        foreach (var mapper in _mappers)
        {
            foreach (var integrationEvent in mapper.Map(domainEvent))
            {
                await integrationEventSink.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
