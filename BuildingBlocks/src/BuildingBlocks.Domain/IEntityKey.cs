namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a unique identifier for an entity or aggregate root in the domain model.
/// </summary>
public interface IEntityKey
{
    /// <summary>
    /// Gets a value indicating whether the entity key is empty.
    /// </summary>
    /// <remarks>
    /// An empty key typically means that the entity has not yet been assigned a valid identifier.
    /// </remarks>
    bool IsEmpty { get; }
}

/// <summary>
/// Represents a strongly typed unique identifier for an entity or aggregate root in the domain model.
/// </summary>
/// <remarks>
/// Strong typing ensures that the key's value is of type <typeparamref name="TValue"/> and cannot be <see langword="null"/>.
/// </remarks>
/// <typeparam name="TValue">The type of the key's value.</typeparam>
public interface IEntityKey<out TValue> : IEntityKey
    where TValue : notnull
{
    /// <summary>
    /// Gets the value of the key.
    /// </summary>
    TValue Value { get; }
}
