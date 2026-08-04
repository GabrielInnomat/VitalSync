using BuildingBlocks.Application;
using JasperFx;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Dispatching;

internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork? unitOfWork = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    public const string ConcurrencyConflictCode = "persistence.concurrency_conflict";

    private static readonly bool IsCommand =
        typeof(ICommand).IsAssignableFrom(typeof(TRequest))
        || Array.Exists(
            typeof(TRequest).GetInterfaces(),
            static @interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(ICommand<>));

    public async Task<TResponse> Handle(TRequest request, RequestPipelineContinuation<TResponse> continuation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        var response = await continuation(cancellationToken).ConfigureAwait(false);

        if (unitOfWork is null || !IsCommand || response.IsFailure)
        {
            return response;
        }

        try
        {
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyException exception)
        {
            return FailureResults.Create<TResponse>(Failure.Conflict(ConcurrencyConflictCode, exception.Message));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            return FailureResults.Create<TResponse>(Failure.Conflict(ConcurrencyConflictCode, exception.Message));
        }

        return response;
    }
}
