namespace BuildingBlocks.Application;

/// <summary>
/// Represents a single, categorised failure carried by a failed <see cref="Result"/>.
/// </summary>
/// <remarks>
/// A failure pairs a stable, machine-readable <see cref="Code"/> (usable for internationalisation and specific client
/// handling) with a human-readable <see cref="Message"/> and an <see cref="FailureCategory"/> that conveys failure
/// semantics to the boundary. Prefer the category-named factory methods (for example <see cref="NotFound"/>) over the
/// constructor so the intent is explicit at the call site.
/// </remarks>
public sealed record Failure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Failure"/> record.
    /// </summary>
    /// <remarks>
    /// Prefer the category-named factory methods over this constructor unless a category is chosen dynamically.
    /// </remarks>
    /// <param name="code">A stable, machine-readable identifier for the failure (for example <c>recipe.name_required</c>).</param>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <param name="category">The category that classifies the failure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="code"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> or <paramref name="message"/> is empty or consists only of white-space characters.</exception>
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

    /// <summary>
    /// Gets the stable, machine-readable identifier for the failure.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable description of the failure.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the category that classifies the failure.
    /// </summary>
    public FailureCategory Category { get; }

    /// <summary>
    /// Creates a <see cref="Failure"/> in the <see cref="FailureCategory.Validation"/> category.
    /// </summary>
    /// <param name="code">A stable, machine-readable identifier for the error.</param>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <returns>A new validation <see cref="Failure"/>.</returns>
    public static Failure Validation(string code, string message) => new(code, message, FailureCategory.Validation);

    /// <summary>
    /// Creates a <see cref="Failure"/> in the <see cref="FailureCategory.BusinessRule"/> category.
    /// </summary>
    /// <param name="code">A stable, machine-readable identifier for the error.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <returns>A new business-rule <see cref="Failure"/>.</returns>
    public static Failure BusinessRule(string code, string message) => new(code, message, FailureCategory.BusinessRule);

    /// <summary>
    /// Creates a <see cref="Failure"/> in the <see cref="FailureCategory.NotFound"/> category.
    /// </summary>
    /// <param name="code">A stable, machine-readable identifier for the error.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <returns>A new not-found <see cref="Failure"/>.</returns>
    public static Failure NotFound(string code, string message) => new(code, message, FailureCategory.NotFound);

    /// <summary>
    /// Creates a <see cref="Failure"/> in the <see cref="FailureCategory.Conflict"/> category.
    /// </summary>
    /// <param name="code">A stable, machine-readable identifier for the error.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <returns>A new conflict <see cref="Failure"/>.</returns>
    public static Failure Conflict(string code, string message) => new(code, message, FailureCategory.Conflict);
}
