using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

internal static class EntityKeyFormatter
{
    private static readonly ConcurrentDictionary<Type, Func<object, object>> ValueAccessors = new();
    private static readonly ConcurrentDictionary<Type, string> AggregateNames = new();

    public static string GetAggregateName(Type aggregateType) =>
        AggregateNames.GetOrAdd(aggregateType, ReadAggregateName);

    public static string GetKeyValue(object key) =>
        string.Create(CultureInfo.InvariantCulture, $"{ReadKeyValue(key)}");

    public static string GetStreamKey(string aggregateName, string keyValue) =>
        string.Create(CultureInfo.InvariantCulture, $"{aggregateName}/{keyValue}");

    private static string ReadAggregateName(Type aggregateType) =>
        aggregateType.GetCustomAttribute<AggregateNameAttribute>(inherit: false)?.Name
        ?? throw new InvalidOperationException(
            $"The aggregate '{aggregateType}' has no [AggregateName]. The name prefixes every event stream and " +
            "travels on every domain event envelope, so it is a persistence contract and must be chosen " +
            "deliberately instead of following the CLR type name (ADR-0030).");

    private static object ReadKeyValue(object key)
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
