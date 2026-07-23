namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a business rule that can be validated.
/// </summary>
public interface IBusinessRule
{
    /// <summary>
    /// Gets the message associated with the business rule.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Determines whether the business rule is broken.
    /// </summary>
    /// <returns></returns>
    bool IsBroken();
}
