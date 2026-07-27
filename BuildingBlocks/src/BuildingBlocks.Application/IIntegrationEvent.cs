namespace BuildingBlocks.Application;

/// <summary>
/// Marks a message as an integration event that communicates a fact across bounded-context boundaries.
/// </summary>
/// <remarks>
/// Integration events are the only cross-context signal: they are translated from selected domain events by an
/// <see cref="IIntegrationEventMapper"/> and published to the message broker by the outbox-backed publisher in
/// <c>BuildingBlocks.Infrastructure</c>. Use this marker on immutable, serializable contract types owned by the
/// publishing service; never expose domain events or aggregates across the broker. Consumers subscribe to integration
/// events instead of reading another context's database.
/// </remarks>
public interface IIntegrationEvent;
