using System.Collections.Concurrent;
using System.Linq.Expressions;
using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Dispatching;

internal static class FailureResults
{
    private static readonly ConcurrentDictionary<Type, Func<Failure, Result>> Factories = new();

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
