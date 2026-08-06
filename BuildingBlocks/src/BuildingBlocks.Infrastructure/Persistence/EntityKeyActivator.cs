using System.Linq.Expressions;
using BuildingBlocks.Domain.Entities;

namespace BuildingBlocks.Infrastructure.Persistence;

internal static class EntityKeyActivator<TKey, TValue>
    where TKey : IEntityKey<TValue>
    where TValue : notnull
{
    private static readonly Lazy<Func<TValue, TKey>> CompiledFactory = new(BuildFactory);

    public static TKey Create(TValue value) => CompiledFactory.Value(value);

    private static Func<TValue, TKey> BuildFactory()
    {
        var constructor = typeof(TKey).GetConstructor([typeof(TValue)])
            ?? throw new InvalidOperationException(
                $"The key type '{typeof(TKey)}' must expose a public constructor taking a single '{typeof(TValue)}' argument.");

        var parameter = Expression.Parameter(typeof(TValue), "value");
        return Expression.Lambda<Func<TValue, TKey>>(Expression.New(constructor, parameter), parameter).Compile();
    }
}
