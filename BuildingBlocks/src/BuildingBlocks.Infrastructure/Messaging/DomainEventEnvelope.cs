namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// The single concrete Wolverine message type used to carry any domain event through the messaging transport's
/// transactional outbox.
/// </summary>
/// <remarks>
/// Wolverine requires a concrete message type for handler routing — it does not dispatch by interface or base type
/// (see <see cref="DomainEventEnvelopeHandler"/>) — while domain events are an open, per-service polymorphic set
/// (<see cref="BuildingBlocks.Domain.IDomainEvent"/>) that this package cannot know ahead of time. This envelope
/// closes that gap: the unit-of-work implementations wrap every domain event in one of these before publishing it, and
/// <see cref="DomainEventEnvelopeSerializer"/> carries the CLR type name alongside the JSON payload so the event can be
/// rehydrated to its exact runtime type on delivery.
/// </remarks>
/// <param name="EventTypeName">The assembly-qualified CLR type name of the wrapped domain event.</param>
/// <param name="Payload">The JSON payload of the wrapped domain event.</param>
public sealed record DomainEventEnvelope(string EventTypeName, string Payload);
