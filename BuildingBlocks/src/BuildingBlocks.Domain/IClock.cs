namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a clock that provides the current date and time.
/// </summary>
/// <remarks>
/// This abstraction over the system clock allows for easier testing and decoupling from the system's actual time.
/// </remarks>
public interface IClock
{
    /// <summary>
    /// Gets the current date and time as a <see cref="DateTimeOffset"/> value.
    /// </summary>
    DateTimeOffset Now { get; }
}
