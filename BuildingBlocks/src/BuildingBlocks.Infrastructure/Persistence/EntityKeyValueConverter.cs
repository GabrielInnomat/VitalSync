using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// EF Core value converter that maps a strongly typed entity key to its underlying primitive value and back.
/// </summary>
/// <remarks>
/// Strongly typed identifiers (ADR-0005) are structs wrapping a primitive; this converter stores the wrapped
/// <see cref="IEntityKey{TValue}.Value"/> in the column and reconstructs the key through its <c>(TValue)</c>
/// constructor on materialization. Apply it per property, or model-wide via
/// <see cref="EntityKeyModelBuilderExtensions.ApplyEntityKeyConversions"/>.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
/// <typeparam name="TValue">The primitive type wrapped by the key.</typeparam>
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

/// <summary>
/// EF Core model conventions that apply the entity-key value conversion across a model.
/// </summary>
/// <remarks>
/// Call <see cref="ApplyEntityKeyConversions"/> at the end of a write-database context's <c>OnModelCreating</c> to
/// convert every property whose CLR type implements <see cref="IEntityKey{TValue}"/> without configuring each
/// property individually. Because a strongly typed key wraps a type EF Core cannot map on its own, the scan works
/// over the entity's CLR properties rather than the already-discovered model properties; this ensures a strongly
/// typed <b>primary key</b> is configured as well, which relational providers otherwise refuse to map.
/// </remarks>
public static class EntityKeyModelBuilderExtensions
{
    private static readonly ConcurrentDictionary<Type, ValueConverter> Converters = new();

    /// <summary>
    /// Applies the <see cref="EntityKeyValueConverter{TKey, TValue}"/> to every strongly typed key property.
    /// </summary>
    /// <param name="modelBuilder">The model builder whose entity types are inspected.</param>
    /// <returns>The same model builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
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

                var propertyBuilder = modelBuilder.Entity(clrType).Property(propertyInfo.Name);
                if (propertyBuilder.Metadata.GetValueConverter() is not null)
                {
                    continue;
                }

                var converter = Converters.GetOrAdd(
                    propertyInfo.PropertyType,
                    static (keyType, valueType) => (ValueConverter)Activator.CreateInstance(
                        typeof(EntityKeyValueConverter<,>).MakeGenericType(keyType, valueType))!,
                    keyInterface.GetGenericArguments()[0]);

                propertyBuilder.HasConversion(converter);
            }
        }

        return modelBuilder;
    }
}
