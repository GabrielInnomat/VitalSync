using System.Collections.Concurrent;
using System.Linq.Expressions;
using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Dispatching;

/// <summary>
/// Creates failed <see cref="Result"/> instances for the closed response type of a pipeline.
/// </summary>
/// <remarks>
/// Pipeline behaviors are generic over <c>TResponse</c> (either <see cref="Result"/> or <see cref="Result{TResult}"/>),
/// so they cannot call the static factory of the concrete type directly. This helper compiles and caches a factory
/// delegate per response type, keeping failure creation allocation-light after first use.
/// </remarks>
internal static class FailureResults
{
    private static readonly ConcurrentDictionary<Type, Func<Failure, Result>> Factories = new();

    /// <summary>
    /// Creates a failed result of type <typeparamref name="TResponse"/> carrying the specified failure.
    /// </summary>
    /// <param name="failure">The failure the result carries.</param>
    /// <typeparam name="TResponse">The concrete result type produced by the pipeline.</typeparam>
    /// <returns>A failed result of type <typeparamref name="TResponse"/>.</returns>
    public static TResponse Create<TResponse>(Failure failure)
        where TResponse : Result
    {
        var factory = Factories.GetOrAdd(typeof(TResponse), CreateFactory);
        return (TResponse)factory(failure);
    }

    private static Func<Failure, Result> CreateFactory(Type responseType)
    {
        if (responseType == typeof(Result))
        {
            return static failure => Result.Failure(failure);
        }

        var method = responseType.GetMethod(nameof(Result.Failure), [typeof(Failure)])
            ?? throw new InvalidOperationException(
                $"The response type '{responseType}' does not expose a static Failure(Failure) factory.");

        var parameter = Expression.Parameter(typeof(Failure), "failure");
        var call = Expression.Convert(Expression.Call(method, parameter), typeof(Result));
        return Expression.Lambda<Func<Failure, Result>>(call, parameter).Compile();
    }
}
