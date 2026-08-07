using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Application.Results;
using JasperFx;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BuildingBlocks.Infrastructure.Dispatching;

internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    public const string ConcurrencyConflictCode = "persistence.concurrency_conflict";

    public const string UniqueViolationCode = "persistence.unique_violation";

    private static readonly bool IsCommand =
        typeof(ICommand).IsAssignableFrom(typeof(TRequest))
        || Array.Exists(
            typeof(TRequest).GetInterfaces(),
            static @interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(ICommand<>));

    public async Task<TResponse> HandleAsync(TRequest request, RequestPipeline<TResponse> pipeline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        var response = await pipeline.NextAsync(cancellationToken).ConfigureAwait(false);

        if (!IsCommand || response.IsFailure)
        {
            return response;
        }

        try
        {
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyException exception)
        {
            return pipeline.Failed(Failure.Conflict(ConcurrencyConflictCode, exception.Message));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            return pipeline.Failed(Failure.Conflict(ConcurrencyConflictCode, exception.Message));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception.InnerException))
        {
            return pipeline.Failed(Failure.Conflict(
                UniqueViolationCode,
                Describe((PostgresException)exception.InnerException!)));
        }
        catch (PostgresException exception) when (IsUniqueViolation(exception))
        {
            return pipeline.Failed(Failure.Conflict(UniqueViolationCode, Describe(exception)));
        }

        return response;
    }

    private static bool IsUniqueViolation(Exception? exception) =>
        exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static string Describe(PostgresException exception) =>
        string.IsNullOrWhiteSpace(exception.ConstraintName)
            ? exception.Message
            : $"The unique constraint '{exception.ConstraintName}' was violated.";
}
