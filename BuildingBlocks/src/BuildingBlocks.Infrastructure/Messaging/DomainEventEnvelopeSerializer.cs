using System.Text.Json;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Messaging;

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
