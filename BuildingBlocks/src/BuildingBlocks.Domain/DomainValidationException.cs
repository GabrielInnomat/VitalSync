namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an exception that is thrown when a domain validation fails.
/// </summary>
public sealed class DomainValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class with a default error message.
    /// </summary>
    public DomainValidationException()
        : base("The domain validation failed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public DomainValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
