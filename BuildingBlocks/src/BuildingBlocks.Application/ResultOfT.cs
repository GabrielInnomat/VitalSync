namespace BuildingBlocks.Application;

/// <summary>
/// Represents the outcome of a command or query as either success carrying a value of type
/// <typeparamref name="TResult"/> or failure carrying one or more <see cref="Error"/>s.
/// </summary>
/// <remarks>
/// Use this for operations that must return data on success, such as a query or a <c>create</c> command that returns
/// the new aggregate's strongly typed identifier. Access <see cref="Value"/> only after checking
/// <see cref="Result.IsSuccess"/>. Both a <typeparamref name="TResult"/> value and an <see cref="Error"/> convert
/// implicitly to a result, keeping handler code terse.
/// </remarks>
/// <typeparam name="TResult">The type of the value carried on success.</typeparam>
public sealed class Result<TResult> : Result
{
	private readonly TResult _value;

	private Result(TResult value)
		: base(true, [])
	{
		_value = value;
	}

	private Result(IReadOnlyList<Error> errors)
		: base(false, errors)
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
	public static new Result<TResult> Success(TResult value) => new(value);

	/// <summary>
	/// Creates a failed result carrying the specified error.
	/// </summary>
	/// <param name="error">The error describing the failure.</param>
	/// <returns>A failed <see cref="Result{TResult}"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
	public static new Result<TResult> Failure(Error error)
	{
		ArgumentNullException.ThrowIfNull(error);
		return new Result<TResult>([error]);
	}

	/// <summary>
	/// Creates a failed result carrying the specified errors.
	/// </summary>
	/// <param name="errors">The errors describing the failure.</param>
	/// <returns>A failed <see cref="Result{TResult}"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty.</exception>
	public static new Result<TResult> Failure(IReadOnlyList<Error> errors) => new(errors);

	/// <summary>
	/// Converts a value into a successful <see cref="Result{TResult}"/>.
	/// </summary>
	/// <param name="value">The value produced by the operation.</param>
	public static implicit operator Result<TResult>(TResult value) => Success(value);

	/// <summary>
	/// Converts an <see cref="Error"/> into a failed <see cref="Result{TResult}"/>.
	/// </summary>
	/// <param name="error">The error describing the failure.</param>
	public static implicit operator Result<TResult>(Error error) => Failure(error);
}
