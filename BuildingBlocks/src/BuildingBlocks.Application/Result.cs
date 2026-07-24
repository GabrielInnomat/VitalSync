using System.Collections.ObjectModel;

namespace BuildingBlocks.Application;

/// <summary>
/// Represents the outcome of a command or query as either success or failure carrying one or more <see cref="Error"/>s.
/// </summary>
/// <remarks>
/// A result gives commands and queries a single, uniform failure channel so callers (the BFF, the frontend) handle
/// expected outcomes consistently instead of juggling exceptions and return values. Create instances with
/// <see cref="Success()"/> or <see cref="Failure(Error)"/>; an <see cref="Error"/> converts implicitly to a failed
/// result. Use <see cref="Result{TResult}"/> when the operation must also return a value on success.
/// </remarks>
public class Result
{
	private static readonly ReadOnlyCollection<Error> NoErrors = new([]);

	private readonly ReadOnlyCollection<Error> _errors;

	/// <summary>
	/// Initializes a new instance of the <see cref="Result"/> class.
	/// </summary>
	/// <param name="isSuccess"><see langword="true"/> for a successful result; otherwise, <see langword="false"/>.</param>
	/// <param name="errors">The errors carried by a failed result, or an empty collection for a successful result.</param>
	/// <exception cref="ArgumentException">Thrown when success is paired with errors, or failure is paired with no errors.</exception>
	protected Result(bool isSuccess, IReadOnlyList<Error> errors)
	{
		ArgumentNullException.ThrowIfNull(errors);

		if (isSuccess && errors.Count > 0)
		{
			throw new ArgumentException("A successful result cannot carry errors.", nameof(errors));
		}

		if (!isSuccess && errors.Count == 0)
		{
			throw new ArgumentException("A failed result must carry at least one error.", nameof(errors));
		}

		IsSuccess = isSuccess;
		_errors = errors.Count == 0 ? NoErrors : new ReadOnlyCollection<Error>([.. errors]);
	}

	/// <summary>
	/// Gets a value indicating whether the operation succeeded.
	/// </summary>
	public bool IsSuccess { get; }

	/// <summary>
	/// Gets a value indicating whether the operation failed.
	/// </summary>
	public bool IsFailure => !IsSuccess;

	/// <summary>
	/// Gets the errors carried by a failed result, or an empty collection when the result is successful.
	/// </summary>
	public IReadOnlyList<Error> Errors => _errors;

	/// <summary>
	/// Creates a successful result.
	/// </summary>
	/// <returns>A successful <see cref="Result"/>.</returns>
	public static Result Success() => new(true, NoErrors);

	/// <summary>
	/// Creates a successful result carrying the specified value.
	/// </summary>
	/// <param name="value">The value produced by the operation.</param>
	/// <typeparam name="TResult">The type of the produced value.</typeparam>
	/// <returns>A successful <see cref="Result{TResult}"/> carrying <paramref name="value"/>.</returns>
	public static Result<TResult> Success<TResult>(TResult value) => Result<TResult>.Success(value);

	/// <summary>
	/// Creates a failed result carrying the specified error.
	/// </summary>
	/// <param name="error">The error describing the failure.</param>
	/// <returns>A failed <see cref="Result"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
	public static Result Failure(Error error)
	{
		ArgumentNullException.ThrowIfNull(error);
		return new Result(false, [error]);
	}

	/// <summary>
	/// Creates a failed result carrying the specified errors.
	/// </summary>
	/// <param name="errors">The errors describing the failure.</param>
	/// <returns>A failed <see cref="Result"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty.</exception>
	public static Result Failure(IReadOnlyList<Error> errors)
	{
		ArgumentNullException.ThrowIfNull(errors);
		return new Result(false, errors);
	}

	/// <summary>
	/// Converts an <see cref="Error"/> into a failed <see cref="Result"/>.
	/// </summary>
	/// <param name="error">The error describing the failure.</param>
	public static implicit operator Result(Error error) => Failure(error);
}
