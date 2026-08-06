using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Entities;

namespace BuildingBlocks.Infrastructure.Persistence;

internal sealed class EntityKeyJsonConverter<TKey, TValue> : JsonConverter<TKey>
    where TKey : IEntityKey<TValue>
    where TValue : notnull
{
    public override TKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = JsonSerializer.Deserialize<TValue>(ref reader, options)
            ?? throw new JsonException(
                $"The strongly typed key '{typeof(TKey)}' cannot be read from a null value.");

        return EntityKeyActivator<TKey, TValue>.Create(value);
    }

    public override void Write(Utf8JsonWriter writer, TKey value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);

        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
