using BuildingBlocks.Application;
using Marten.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Dispatching;

/// <summary>
/// Pipeline behavior that owns the unit of work for command dispatches.
/// </summary>
/// <remarks>
/// Exactly one unit of work spans each command: the behavior invokes the rest of the pipeline and, when the handler
/// returns a successful result, commits the <see cref="IUnitOfWork"/> — persisting the aggregate changes together with
/// the transactional outbox write. On a failed result nothing is committed, which rolls the work back. Queries bypass
/// the behavior entirely, so no unit of work needs to be registered for query-only hosts. Optimistic-concurrency
/// conflicts raised by the store on commit (Marten's <see cref="ConcurrencyException"/>, EF Core's
/// <see cref="DbUpdateConcurrencyException"/>) are translated into a <see cref="FailureCategory.Conflict"/> failure
/// per ADR-0019. Register this behavior <b>last</b>, closest to the handler.
/// </remarks>
/// <typeparam name="TRequest">The type of the request flowing through the pipeline.</typeparam>
/// <typeparam name="TResponse">The type of the result produced by the pipeline.</typeparam>
/// <param name="serviceProvider">The scoped service provider used to resolve the unit of work for commands.</param>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IServiceProvider serviceProvider)
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

        if (!IsCommand || response.IsFailure)
        {
            return response;
        }

        var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();

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
