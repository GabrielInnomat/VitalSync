namespace BuildingBlocks.Application;

public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, RequestPipelineContinuation<TResponse> continuation, CancellationToken cancellationToken);
}
