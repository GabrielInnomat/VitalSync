namespace BuildingBlocks.Application;

/// <summary>
/// Represents the outcome of a command or query as either success carrying a value of type
/// <typeparamref name="TResult"/> or failure carrying one or more <see cref="Application.Failure"/>s.
/// </summary>
/// <remarks>
/// Use this for operations that must return data on success, such as a query or a <c>create</c> command that returns
/// the new aggregate's strongly typed identifier. Access <see cref="Value"/> only after checking
/// <see cref="Result.IsSuccess"/>. Both a <typeparamref name="TResult"/> value and an <see cref="Application.Failure"/> convert
/// implicitly to a result, keeping handler code terse.
/// </remarks>
/// <typeparam name="TResult">The type of the value carried on success.</typeparam>
public sealed class Result<TResult> : Result
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "A backing field is required because the Value property validates the result state before returning the stored value")]
    private readonly TResult _value;

    private Result(TResult value)
        : base(true, [])
    {
        _value = value;
    }

    private Result(IReadOnlyList<Failure> failures)
        : base(false, failures)
    {
        _value = default!;
    }

    /// <summary>
    /// Gets the value produced by a successful operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result represents a failure.</exception>
    public TResult Value =>
        IsSuccess
            ? _value
            : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>
    /// Creates a successful result carrying the specified value.
    /// </summary>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>A successful <see cref="Result{TResult}"/> carrying <paramref name="value"/>.</returns>
    public static Result<TResult> Success(TResult value) => new(value);

    /// <summary>
    /// Creates a failed result carrying the specified failure.
    /// </summary>
    /// <param name="failure">The relevant failure information.</param>
    /// <returns>A failed <see cref="Result{TResult}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="failure"/> is <see langword="null"/>.</exception>
    public static new Result<TResult> Failure(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new Result<TResult>([failure]);
    }

    /// <summary>
    /// Creates a failed result carrying the specified failures.
    /// </summary>
    /// <param name="failures">The failures list.</param>
    /// <returns>A failed <see cref="Result{TResult}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="failures"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="failures"/> is empty.</exception>
    public static new Result<TResult> Failure(IReadOnlyList<Failure> failures) => new(failures);

    /// <summary>
    /// Converts a value into a successful <see cref="Result{TResult}"/>.
    /// </summary>
    /// <param name="value">The value produced by the operation.</param>
    public static implicit operator Result<TResult>(TResult value) => Success(value);

    /// <summary>
    /// Converts an <see cref="Application.Failure"/> into a failed <see cref="Result{TResult}"/>.
    /// </summary>
    /// <param name="failure">The failure to convert.</param>
    public static implicit operator Result<TResult>(Failure failure) => Failure(failure);
}
