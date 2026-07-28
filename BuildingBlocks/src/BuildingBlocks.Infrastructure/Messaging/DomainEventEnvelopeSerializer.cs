using System.Text.Json;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Wraps domain events into <see cref="DomainEventEnvelope"/>s and back.
/// </summary>
/// <remarks>
/// Events are serialized together with their assembly-qualified CLR type name, so the envelope handler can rehydrate
/// the concrete event type without any registry. Domain events must therefore be System.Text.Json-serializable
/// records, which the <c>DomainEvent</c> base already guarantees for its own members.
/// </remarks>
internal static class DomainEventEnvelopeSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);

    public static DomainEventEnvelope Wrap(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();
        var eventTypeName = eventType.AssemblyQualifiedName
            ?? throw new InvalidOperationException($"The domain event type '{eventType}' has no assembly-qualified name.");

        return new DomainEventEnvelope(eventTypeName, JsonSerializer.Serialize(domainEvent, eventType, SerializerOptions));
    }

    public static IDomainEvent Unwrap(DomainEventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var eventType = Type.GetType(envelope.EventTypeName, throwOnError: true)!;
        var domainEvent = JsonSerializer.Deserialize(envelope.Payload, eventType, SerializerOptions)
            ?? throw new InvalidOperationException(
                $"The domain event envelope payload deserialized to null for type '{envelope.EventTypeName}'.");

        return (IDomainEvent)domainEvent;
    }
}
