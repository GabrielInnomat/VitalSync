namespace BuildingBlocks.Application;

public delegate Task<TResponse> RequestPipelineContinuation<TResponse>(CancellationToken cancellationToken);
