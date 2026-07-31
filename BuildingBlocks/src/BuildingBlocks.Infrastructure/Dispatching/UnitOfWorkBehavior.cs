using BuildingBlocks.Application;
using JasperFx;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Dispatching;

/// <summary>
/// Pipeline behavior that owns the unit of work for command dispatches.
/// </summary>
/// <remarks>
/// Exactly one unit of work spans each command: the behavior invokes the rest of the pipeline and, when the handler
/// returns a successful result, commits the <see cref="IUnitOfWork"/> — persisting the aggregate changes together with
/// the transactional outbox write. On a failed result nothing is committed, which rolls the work back. Queries bypass
/// the behavior entirely, so no unit of work needs to be registered for query-only hosts. The unit of work is an
/// <b>optional</b> dependency: a host without configured persistence (handler unit tests, gateway/facade services,
/// services with their own persistence) resolves it as <see langword="null"/> and commands pass through without a
/// commit — the optionality is visible in the constructor signature instead of failing at dispatch time, and the
/// behavior can be instantiated directly in tests without a container. Optimistic-concurrency
/// conflicts raised by the store on commit (Marten's <see cref="ConcurrencyException"/>, EF Core's
/// <see cref="DbUpdateConcurrencyException"/>) are translated into a <see cref="FailureCategory.Conflict"/> failure
/// per ADR-0019. It is the <b>innermost</b> built-in behavior
/// (<see cref="DependencyInjection.BuildingBlocksOptions.UnitOfWorkBehaviorOrder"/>), running closest to the handler.
/// </remarks>
/// <typeparam name="TRequest">The type of the request flowing through the pipeline.</typeparam>
/// <typeparam name="TResponse">The type of the result produced by the pipeline.</typeparam>
/// <param name="unitOfWork">The unit of work to commit for successful commands, or <see langword="null"/> when the host has no configured persistence.</param>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork? unitOfWork = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    /// <summary>
    /// The stable failure code used for optimistic-concurrency conflicts detected on commit.
    /// </summary>
    public const string ConcurrencyConflictCode = "persistence.concurrency_conflict";

    private static readonly bool IsCommand =
        typeof(ICommand).IsAssignableFrom(typeof(TRequest))
        || Array.Exists(
            typeof(TRequest).GetInterfaces(),
            static @interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(ICommand<>));

    /// <inheritdoc/>
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
