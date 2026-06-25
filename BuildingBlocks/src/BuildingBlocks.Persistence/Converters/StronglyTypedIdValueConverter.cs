using BuildingBlocks.Domain.Identifiers;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Persistence.Converters;

/// <summary>
/// Converts a strongly typed identifier to and from its underlying primitive
/// value for EF Core persistence.
/// </summary>
/// <typeparam name="TId">The strongly typed identifier type.</typeparam>
/// <typeparam name="TValue">The underlying primitive value type.</typeparam>
public sealed class StronglyTypedIdValueConverter<TId, TValue> : ValueConverter<TId, TValue>
    where TId : StronglyTypedId<TId, TValue>
    where TValue : notnull, IComparable<TValue>
{
    /// <summary>Initializes the converter using the supplied factory for the id type.</summary>
    /// <param name="factory">A factory that builds a <typeparamref name="TId"/> from a primitive value.</param>
    public StronglyTypedIdValueConverter(Func<TValue, TId> factory)
        : base(id => id.Value, value => factory(value))
    {
    }
}