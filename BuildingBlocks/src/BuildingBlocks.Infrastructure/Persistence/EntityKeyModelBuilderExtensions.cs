using System.Collections.Concurrent;
using BuildingBlocks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Persistence;

public static class EntityKeyModelBuilderExtensions
{
    private static readonly ConcurrentDictionary<Type, ValueConverter> Converters = new();

    public static ModelBuilder ApplyEntityKeyConversions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                ApplyConverter(property);
            }
        }

        return modelBuilder;
    }

    private static void ApplyConverter(IMutableProperty property)
    {
        if (property.GetValueConverter() is not null)
        {
            return;
        }

        var keyInterface = Array.Find(
            property.ClrType.GetInterfaces(),
            static @interface => @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IEntityKey<>));

        if (keyInterface is null)
        {
            return;
        }

        property.SetValueConverter(Converters.GetOrAdd(
            property.ClrType,
            static (keyType, valueType) => (ValueConverter)Activator.CreateInstance(
                typeof(EntityKeyValueConverter<,>).MakeGenericType(keyType, valueType))!,
            keyInterface.GetGenericArguments()[0]));
    }
}
