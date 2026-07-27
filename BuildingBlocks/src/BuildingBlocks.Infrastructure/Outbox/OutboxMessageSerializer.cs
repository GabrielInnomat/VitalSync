using System.Text.Json;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Serializes domain events into outbox payloads and back.
/// </summary>
/// <remarks>
/// Events are stored as JSON together with their assembly-qualified CLR type name, so the drain loop can rehydrate the
/// concrete event type without any registry. Domain events must therefore be System.Text.Json-serializable records,
/// which the <see cref="DomainEvent"/> base already guarantees for its own members.
/// </remarks>
internal static class OutboxMessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);

    public static string Serialize(IDomainEvent domainEvent) =>
        JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);

    public static string GetEventTypeName(IDomainEvent domainEvent) =>
        domainEvent.GetType().AssemblyQualifiedName
        ?? throw new InvalidOperationException(
            $"The domain event type '{domainEvent.GetType()}' has no assembly-qualified name.");

    public static IDomainEvent Deserialize(OutboxMessage message)
    {
        var eventType = Type.GetType(message.EventType, throwOnError: true)!;
        var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType, SerializerOptions)
            ?? throw new InvalidOperationException(
                $"The outbox payload of message '{message.Id}' deserialized to null.");
        return (IDomainEvent)domainEvent;
    }
}
