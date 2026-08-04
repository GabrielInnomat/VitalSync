using System.Collections.Concurrent;
using System.Reflection;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Persistence;

public static class EntityKeyModelBuilderExtensions
{
    private static readonly ConcurrentDictionary<Type, ValueConverter> Converters = new();

    public static ModelBuilder ApplyEntityKeyConversions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;
            if (clrType is null)
            {
                continue;
            }

            foreach (var propertyInfo in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.GetIndexParameters().Length != 0 || propertyInfo.GetMethod is null)
                {
                    continue;
                }

                var keyInterface = Array.Find(
                    propertyInfo.PropertyType.GetInterfaces(),
                    static @interface => @interface.IsGenericType
                        && @interface.GetGenericTypeDefinition() == typeof(IEntityKey<>));

                if (keyInterface is null)
                {
                    continue;
                }

                var property = entityType.FindProperty(propertyInfo.Name);

                if (property is null)
                {
                    if (entityType.FindNavigation(propertyInfo.Name) is not null)
                    {
                        continue;
                    }

                    property = entityType.AddProperty(propertyInfo);
                }

                if (property.GetValueConverter() is not null)
                {
                    continue;
                }

                property.SetValueConverter(Converters.GetOrAdd(
                    propertyInfo.PropertyType,
                    static (keyType, valueType) => (ValueConverter)Activator.CreateInstance(
                        typeof(EntityKeyValueConverter<,>).MakeGenericType(keyType, valueType))!,
                    keyInterface.GetGenericArguments()[0]));
            }
        }

        return modelBuilder;
    }
}
