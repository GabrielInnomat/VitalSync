using BuildingBlocks.Application;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Dispatching;

internal sealed class ExceptionToResultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    public const string ValidationFailureCode = "domain.validation";

    public const string BusinessRuleFailureCode = "domain.business_rule";

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
