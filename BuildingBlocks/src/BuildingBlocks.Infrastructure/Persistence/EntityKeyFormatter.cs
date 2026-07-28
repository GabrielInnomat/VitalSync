using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Formats strongly typed entity keys into stable stream identifiers.
/// </summary>
/// <remarks>
/// Stream keys have the shape <c>{AggregateTypeName}/{KeyValue}</c>, which keeps streams of different aggregate types
/// from colliding in the same event store. Value extraction goes through the <see cref="IEntityKey{TValue}"/>
/// contract and is compiled and cached per key type.
/// </remarks>
internal static class EntityKeyFormatter
{
    private static readonly ConcurrentDictionary<Type, Func<object, object>> ValueAccessors = new();

    public static string GetStreamKey(Type aggregateType, object key) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{aggregateType.Name}/{GetKeyValue(key)}");

    private static object GetKeyValue(object key)
    {
        var accessor = ValueAccessors.GetOrAdd(key.GetType(), CreateValueAccessor);
        return accessor(key);
    }

    private static Func<object, object> CreateValueAccessor(Type keyType)
    {
        var keyInterface = Array.Find(
            keyType.GetInterfaces(),
            static @interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEntityKey<>))
            ?? throw new InvalidOperationException(
                $"The key type '{keyType}' does not implement IEntityKey<TValue>.");

        var valueProperty = keyInterface.GetProperty(nameof(IEntityKey<>.Value))!;
        var parameter = Expression.Parameter(typeof(object), "key");
        var body = Expression.Convert(
            Expression.Property(Expression.Convert(parameter, keyInterface), valueProperty),
            typeof(object));
        return Expression.Lambda<Func<object, object>>(body, parameter).Compile();
    }
}
