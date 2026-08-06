using BuildingBlocks.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Persistence;

internal sealed class EntityKeyValueConverter<TKey, TValue>() : ValueConverter<TKey, TValue>(
    key => key.Value,
    value => EntityKeyActivator<TKey, TValue>.Create(value))
    where TKey : struct, IEntityKey<TValue>, IEquatable<TKey>
    where TValue : notnull
{
}
