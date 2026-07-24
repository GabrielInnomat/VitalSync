using System.Collections.ObjectModel;

namespace BuildingBlocks.Application;

/// <summary>
/// Represents the outcome of a command or query as either success or failure carrying one or more <see cref="Application.Failure"/>s.
/// </summary>
/// <remarks>
/// A result gives commands and queries a single, uniform failure channel so callers (the BFF, the frontend) handle
/// expected outcomes consistently instead of juggling exceptions and return values. Create instances with
/// <see cref="Success()"/> or <see cref="Failure(Failure)"/>; an <see cref="Application.Failure"/> converts implicitly to a failed
/// result. Use <see cref="Result{TResult}"/> when the operation must also return a value on success.
/// </remarks>
public class Result
{
    private static readonly ReadOnlyCollection<Failure> NoFailures = new([]);

    private readonly ReadOnlyCollection<Failure> _failures;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess"><see langword="true"/> for a successful result; otherwise, <see langword="false"/>.</param>
    /// <param name="failures">The failures carried by a failed result, or an empty collection for a successful result.</param>
    /// <exception cref="ArgumentException">Thrown when success is paired with failures, or failure list is empty.</exception>
    protected Result(bool isSuccess, IReadOnlyList<Failure> failures)
    {
        if (failures == null)
        {
            throw new ArgumentException("The failures collection cannot be null.", nameof(failures));
        }

        if (isSuccess && failures.Count > 0)
        {
            throw new ArgumentException("A successful result cannot carry failures.", nameof(failures));
        }

        if (!isSuccess && failures.Count == 0)
        {
            throw new ArgumentException("A failed result must carry at least one failure.", nameof(failures));
        }

        IsSuccess = isSuccess;
        _failures = failures.Count == 0 ? NoFailures : new ReadOnlyCollection<Failure>([.. failures]);
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
    /// Gets the failures carried by a failed result, or an empty collection when the result is successful.
    /// </summary>
    public IReadOnlyList<Failure> Failures => _failures;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(true, NoFailures);

    /// <summary>
    /// Creates a successful result carrying the specified value.
    /// </summary>
    /// <param name="value">The value produced by the operation.</param>
    /// <typeparam name="TResult">The type of the produced value.</typeparam>
    /// <returns>A successful <see cref="Result{TResult}"/> carrying <paramref name="value"/>.</returns>
    public static Result<TResult> Success<TResult>(TResult value) => Result<TResult>.Success(value);

    /// <summary>
    /// Creates a failed result carrying the specified failure.
    /// </summary>
    /// <param name="failure">The relevant failure information.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="failure"/> is <see langword="null"/>.</exception>
    public static Result Failure(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new Result(false, [failure]);
    }

    /// <summary>
    /// Creates a failed result carrying the specified failures.
    /// </summary>
    /// <param name="failures">The failures list.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="failures"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="failures"/> is empty.</exception>
    public static Result Failure(IReadOnlyList<Failure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        return new Result(false, failures);
    }

    /// <summary>
    /// Converts an <see cref="Application.Failure"/> into a failed <see cref="Result"/>.
    /// </summary>
    /// <param name="failure">The failure to convert into a failed result.</param>
    public static implicit operator Result(Failure failure) => Failure(failure);
}
