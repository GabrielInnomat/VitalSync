using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class EntityKeyValueConverter<TKey, TValue>() : ValueConverter<TKey, TValue>(
    key => key.Value,
    value => KeyFactory(value))
    where TKey : struct, IEntityKey<TValue>
    where TValue : notnull
{
    private static readonly Func<TValue, TKey> CompiledFactory = BuildFactory();

    private static TKey KeyFactory(TValue value) => CompiledFactory(value);

    private static Func<TValue, TKey> BuildFactory()
    {
        var constructor = typeof(TKey).GetConstructor([typeof(TValue)])
            ?? throw new InvalidOperationException(
                $"The key type '{typeof(TKey)}' must expose a public constructor taking a single '{typeof(TValue)}' argument.");

        var parameter = Expression.Parameter(typeof(TValue), "value");
        return Expression.Lambda<Func<TValue, TKey>>(Expression.New(constructor, parameter), parameter).Compile();
    }
}

public static class EntityKeyModelBuilderExtensions
{
    private static readonly ConcurrentDictionary<Type, ValueConverter> Converters = new();

    public static Microsoft.EntityFrameworkCore.ModelBuilder ApplyEntityKeyConversions(
        this Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
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
