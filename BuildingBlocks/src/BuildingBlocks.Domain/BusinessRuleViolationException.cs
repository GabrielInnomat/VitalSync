namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an exception that is thrown when a business rule is violated.
/// </summary>
/// <remarks>
/// This exception signals that a domain invariant modelled by an <see cref="IBusinessRule"/> was broken, distinguishing
/// business-rule failures from structural validation failures (<see cref="DomainValidationException"/>). It is normally
/// raised through <see cref="RuleChecker"/> rather than thrown directly, so callers can catch it to translate a broken
/// invariant into an appropriate response.
/// </remarks>
public sealed class BusinessRuleViolationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class with a default error message.
    /// </summary>
    public BusinessRuleViolationException()
        : base("A business rule was violated.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BusinessRuleViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
