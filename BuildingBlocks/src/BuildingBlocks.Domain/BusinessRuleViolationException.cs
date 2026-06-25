namespace BuildingBlocks.Domain;

public sealed class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message) : base(message) { }
    public BusinessRuleViolationException(string message, Exception innerException)
        : base(message, innerException) { }
}
