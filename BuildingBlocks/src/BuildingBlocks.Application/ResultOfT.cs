namespace BuildingBlocks.Application;

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

    public TResult Value =>
        IsSuccess
            ? _value
            : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static Result<TResult> Success(TResult value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<TResult>(value);
    }

    public static new Result<TResult> Failure(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new Result<TResult>([failure]);
    }

    public static new Result<TResult> Failure(IReadOnlyList<Failure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        return new Result<TResult>(failures);
    }

    public static implicit operator Result<TResult>(TResult value) => Success(value);

    public static implicit operator Result<TResult>(Failure failure) => Failure(failure);
}
