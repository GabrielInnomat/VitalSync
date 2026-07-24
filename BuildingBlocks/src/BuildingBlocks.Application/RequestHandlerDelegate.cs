namespace BuildingBlocks.Application;

/// <summary>
/// Represents the continuation that invokes the next component in the request pipeline.
/// </summary>
/// <remarks>
/// A pipeline behavior receives this delegate and calls it to pass control to the next behavior or, ultimately, the
/// request handler, then may inspect or transform the returned <typeparamref name="TResponse"/>. Not calling it
/// short-circuits the pipeline and prevents the handler from running.
/// </remarks>
/// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
/// <typeparam name="TResponse">The type of the response produced by the remainder of the pipeline.</typeparam>
/// <returns>A task whose result is the response produced by the next component in the pipeline.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
