using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;

namespace BuildingBlocks.Infrastructure.Events;

/// <summary>
/// The outbox-backed publisher: fans a committed domain event out to projections and the integration-event path.
/// </summary>
/// <remarks>
/// Invoked by the outbox drain loop once per committed event (ADR-0022), the publisher first runs the in-context
/// projection handlers via the <see cref="ProjectionRunner"/> and then translates the event through every registered
/// <see cref="IIntegrationEventMapper"/>, publishing the resulting integration events to the messaging transport
/// (ADR-0023). Because the drain marks an outbox entry processed only after this method succeeds, delivery is
/// at-least-once and everything invoked here must be idempotent.
/// </remarks>
/// <param name="projectionRunner">The runner that dispatches the event to in-context projection handlers.</param>
/// <param name="mappers">The service-owned translation maps from domain events to integration events.</param>
/// <param name="transport">The transport that carries integration events to the broker.</param>
public sealed class Publisher(
    ProjectionRunner projectionRunner,
    IEnumerable<IIntegrationEventMapper> mappers,
    IIntegrationEventTransport transport) : IDomainEventPublisher
{
    private readonly IIntegrationEventMapper[] _mappers = [.. mappers];

    /// <inheritdoc/>
    public async Task PublishAsync(IDomainEvent domainEvent, long streamPosition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await projectionRunner.RunAsync(domainEvent, streamPosition, cancellationToken).ConfigureAwait(false);

        foreach (var mapper in _mappers)
        {
            foreach (var integrationEvent in mapper.Map(domainEvent))
            {
                await transport.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
