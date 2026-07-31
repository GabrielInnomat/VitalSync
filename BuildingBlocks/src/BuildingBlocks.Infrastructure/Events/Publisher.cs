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
/// <see cref="IIntegrationEventMapper"/>, publishing the resulting integration events to the caller-supplied
/// <see cref="IIntegrationEventSink"/> — bound to the transaction of the message being handled, so nothing leaks
/// when the handling fails. Delivery is at-least-once, so everything invoked here must be idempotent.
/// </remarks>
/// <param name="projectionRunner">The runner that dispatches the event to in-context projection handlers.</param>
/// <param name="mappers">The service-owned translation maps from domain events to integration events.</param>
internal sealed class Publisher(
    ProjectionRunner projectionRunner,
    IEnumerable<IIntegrationEventMapper> mappers) : IDomainEventPublisher
{
    private readonly IIntegrationEventMapper[] _mappers = [.. mappers];

    /// <inheritdoc/>
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
