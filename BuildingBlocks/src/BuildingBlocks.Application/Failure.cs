namespace BuildingBlocks.Application;

public sealed record Failure
{
    public Failure(string code, string message, FailureCategory category)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Category = category;
    }

    public string Code { get; }

    public string Message { get; }

    public FailureCategory Category { get; }

    public static Failure Validation(string code, string message) => new(code, message, FailureCategory.Validation);

    public static Failure BusinessRule(string code, string message) => new(code, message, FailureCategory.BusinessRule);

    public static Failure NotFound(string code, string message) => new(code, message, FailureCategory.NotFound);

    public static Failure Conflict(string code, string message) => new(code, message, FailureCategory.Conflict);
}
