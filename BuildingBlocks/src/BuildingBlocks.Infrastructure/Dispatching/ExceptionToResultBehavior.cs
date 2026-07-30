using BuildingBlocks.Application;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Dispatching;

/// <summary>
/// Pipeline behavior that translates expected domain exceptions into failed <see cref="Result"/>s.
/// </summary>
/// <remarks>
/// Per ADR-0017 the domain throws <see cref="DomainValidationException"/> and
/// <see cref="BusinessRuleViolationException"/> for expected errors, while callers consume a uniform
/// <see cref="Result"/> channel; this behavior performs that translation, mapping validation failures to
/// <see cref="FailureCategory.Validation"/> and broken invariants to <see cref="FailureCategory.BusinessRule"/>.
/// It runs inside the logging behavior and outside the unit of work
/// (<see cref="DependencyInjection.BuildingBlocksOptions.ExceptionToResultBehaviorOrder"/>), so expected domain
/// exceptions become failed results before logging sees them and before any transaction is committed. Any other
/// exception is unexpected and passes through untouched to the host's global exception handler.
/// </remarks>
/// <typeparam name="TRequest">The type of the request flowing through the pipeline.</typeparam>
/// <typeparam name="TResponse">The type of the result produced by the pipeline.</typeparam>
public sealed class ExceptionToResultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    /// <summary>
    /// The stable failure code used for translated <see cref="DomainValidationException"/>s.
    /// </summary>
    public const string ValidationFailureCode = "domain.validation";

    /// <summary>
    /// The stable failure code used for translated <see cref="BusinessRuleViolationException"/>s.
    /// </summary>
    public const string BusinessRuleFailureCode = "domain.business_rule";

    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestPipelineContinuation<TResponse> continuation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        try
        {
            return await continuation(cancellationToken).ConfigureAwait(false);
        }
        catch (DomainValidationException exception)
        {
            return FailureResults.Create<TResponse>(Failure.Validation(ValidationFailureCode, exception.Message));
        }
        catch (BusinessRuleViolationException exception)
        {
            return FailureResults.Create<TResponse>(Failure.BusinessRule(BusinessRuleFailureCode, exception.Message));
        }
    }
}
