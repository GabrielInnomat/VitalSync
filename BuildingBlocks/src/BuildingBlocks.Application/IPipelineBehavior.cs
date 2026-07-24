namespace BuildingBlocks.Application;

/// <summary>
/// Wraps the execution of a request handler to apply a cross-cutting concern.
/// </summary>
/// <remarks>
/// Behaviors form an ordered chain around each handler, enabling concerns such as exception-to-<see cref="Result"/>
/// translation, logging, validation, and unit-of-work management without polluting handlers. Only the contract lives
/// here; the concrete behaviors and their DI registration reside in <c>BuildingBlocks.Infrastructure</c> and
/// <c>BuildingBlocks.Persistence</c>. Behaviors run in explicit DI registration order, so register the
/// exception-translation behavior first.
/// </remarks>
/// <typeparam name="TRequest">The type of the request flowing through the pipeline.</typeparam>
/// <typeparam name="TResponse">The type of the response produced by the pipeline.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
	/// <summary>
	/// Handles the request and invokes the next component in the pipeline.
	/// </summary>
	/// <param name="request">The request being processed.</param>
	/// <param name="next">The continuation that invokes the next behavior or the request handler.</param>
	/// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
	/// <returns>A task whose result is the response produced by the pipeline.</returns>
	Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
