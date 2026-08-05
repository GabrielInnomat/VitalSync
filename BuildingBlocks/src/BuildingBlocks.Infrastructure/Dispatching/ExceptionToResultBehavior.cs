using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Rules;

namespace BuildingBlocks.Infrastructure.Dispatching;

internal sealed class ExceptionToResultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    public const string ValidationFailureCode = "domain.validation";

    public const string BusinessRuleFailureCode = "domain.business_rule";

    public async Task<TResponse> HandleAsync(TRequest request, RequestPipeline<TResponse> pipeline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        try
        {
            return await pipeline.NextAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DomainValidationException exception)
        {
            return pipeline.Failed(Failure.Validation(ValidationFailureCode, exception.Message));
        }
        catch (BusinessRuleViolationException exception)
        {
            return pipeline.Failed(Failure.BusinessRule(BusinessRuleFailureCode, exception.Message));
        }
    }
}
