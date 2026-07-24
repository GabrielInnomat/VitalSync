namespace BuildingBlocks.Application;

/// <summary>
/// Represents a single, categorised failure carried by a failed <see cref="Result"/>.
/// </summary>
/// <remarks>
/// An error pairs a stable, machine-readable <see cref="Code"/> (usable for internationalisation and specific client
/// handling) with a human-readable <see cref="Message"/> and an <see cref="ErrorCategory"/> that conveys failure
/// semantics to the boundary. Prefer the category-named factory methods (for example <see cref="NotFound"/>) over the
/// constructor so the intent is explicit at the call site.
/// </remarks>
public sealed record Error
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Error"/> record.
	/// </summary>
	/// <remarks>
	/// Prefer the category-named factory methods over this constructor unless a category is chosen dynamically.
	/// </remarks>
	/// <param name="code">A stable, machine-readable identifier for the error (for example <c>recipe.name_required</c>).</param>
	/// <param name="message">A human-readable description of the error.</param>
	/// <param name="category">The category that classifies the error.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="code"/> or <paramref name="message"/> is <see langword="null"/> or white space.</exception>
	public Error(string code, string message, ErrorCategory category)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(code);
		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		Code = code;
		Message = message;
		Category = category;
	}

	/// <summary>
	/// Gets the stable, machine-readable identifier for the error.
	/// </summary>
	public string Code { get; }

	/// <summary>
	/// Gets the human-readable description of the error.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// Gets the category that classifies the error.
	/// </summary>
	public ErrorCategory Category { get; }

	/// <summary>
	/// Creates a <see cref="Error"/> in the <see cref="ErrorCategory.Validation"/> category.
	/// </summary>
	/// <param name="code">A stable, machine-readable identifier for the error.</param>
	/// <param name="message">A human-readable description of the error.</param>
	/// <returns>A new validation <see cref="Error"/>.</returns>
	public static Error Validation(string code, string message) => new(code, message, ErrorCategory.Validation);

	/// <summary>
	/// Creates a <see cref="Error"/> in the <see cref="ErrorCategory.BusinessRule"/> category.
	/// </summary>
	/// <param name="code">A stable, machine-readable identifier for the error.</param>
	/// <param name="message">A human-readable description of the error.</param>
	/// <returns>A new business-rule <see cref="Error"/>.</returns>
	public static Error BusinessRule(string code, string message) => new(code, message, ErrorCategory.BusinessRule);

	/// <summary>
	/// Creates a <see cref="Error"/> in the <see cref="ErrorCategory.NotFound"/> category.
	/// </summary>
	/// <param name="code">A stable, machine-readable identifier for the error.</param>
	/// <param name="message">A human-readable description of the error.</param>
	/// <returns>A new not-found <see cref="Error"/>.</returns>
	public static Error NotFound(string code, string message) => new(code, message, ErrorCategory.NotFound);

	/// <summary>
	/// Creates a <see cref="Error"/> in the <see cref="ErrorCategory.Conflict"/> category.
	/// </summary>
	/// <param name="code">A stable, machine-readable identifier for the error.</param>
	/// <param name="message">A human-readable description of the error.</param>
	/// <returns>A new conflict <see cref="Error"/>.</returns>
	public static Error Conflict(string code, string message) => new(code, message, ErrorCategory.Conflict);
}
