using System.Linq.Expressions;
using BuildingBlocks.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Persistence;

internal sealed class EntityKeyValueConverter<TKey, TValue>() : ValueConverter<TKey, TValue>(
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
