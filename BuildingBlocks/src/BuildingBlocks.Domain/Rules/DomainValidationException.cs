namespace BuildingBlocks.Domain.Rules;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException()
        : base("The domain validation failed.")
    {
    }

    public DomainValidationException(string message)
        : base(message)
    {
    }

    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
