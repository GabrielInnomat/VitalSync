namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a unique identifier for an entity / aggregate root in the domain model.
/// </summary>
public interface IEntityKey
{
    /// <summary>
    /// Indicates whether the entity key is empty or not.
    /// An empty key typically means that the entity has not been assigned a valid identifier yet.
    /// </summary>
    bool IsEmpty { get; }
}

/// <summary>
/// Represents a unique identifier for an entity / aggregate root in the domain model, with a specific value type.
/// This is used to strongly type the entity key, ensuring that the key's value is of a specific type (TValue) and cannot be null.
/// </summary>
/// <typeparam name="TValue">The type of the value for the key.</typeparam>
public interface IEntityKey<out TValue> : IEntityKey
    where TValue : notnull
{
    /// <summary>
    /// Gets the value of the key.
    /// </summary>
    TValue Value { get; }
}
