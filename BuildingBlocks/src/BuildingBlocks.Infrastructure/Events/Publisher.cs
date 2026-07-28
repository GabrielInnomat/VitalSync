using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;

namespace BuildingBlocks.Infrastructure.Events;

/// <summary>
/// The outbox-backed publisher: fans a committed domain event out to projections and the integration-event path.
/// </summary>
/// <remarks>
/// Invoked once per domain event delivered by Wolverine's transactional outbox (ADR-0022/0023) — see
/// <see cref="DomainEventEnvelopeHandler"/> — the publisher first runs the in-context projection handlers
/// via the <see cref="ProjectionRunner"/> and then translates the event through every registered
/// <see cref="IIntegrationEventMapper"/>, publishing the resulting integration events to the messaging transport.
/// Delivery is at-least-once, so everything invoked here must be idempotent.
/// </remarks>
/// <param name="projectionRunner">The runner that dispatches the event to in-context projection handlers.</param>
/// <param name="mappers">The service-owned translation maps from domain events to integration events.</param>
/// <param name="transport">The transport that carries integration events to the broker.</param>
internal sealed class Publisher(
    ProjectionRunner projectionRunner,
    IEnumerable<IIntegrationEventMapper> mappers,
    IIntegrationEventTransport transport) : IDomainEventPublisher
{
    private readonly IIntegrationEventMapper[] _mappers = [.. mappers];

    /// <inheritdoc/>
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await projectionRunner.RunAsync(domainEvent, cancellationToken).ConfigureAwait(false);

        foreach (var mapper in _mappers)
        {
            foreach (var integrationEvent in mapper.Map(domainEvent))
            {
                await transport.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
