namespace BuildingBlocks.Domain;

/// <summary>
/// Marker for strongly-typed entity keys. The domain uses this abstraction so
/// aggregates and entities are never coupled to a specific underlying identity
/// type. Use <see cref="IEntityKey{TValue}"/> to expose the concrete value.
/// </summary>
public interface IEntityKey
{
}

/// <summary>
/// A strongly-typed entity key backed by an underlying <typeparamref name="TValue"/>.
/// This keeps keys type-safe without permanently locking identity to <see cref="Guid"/>.
/// </summary>
public interface IEntityKey<out TValue> : IEntityKey
    where TValue : notnull
{
    TValue Value { get; }
}
