namespace BuildingBlocks.Application;

/// <summary>
/// Dispatches commands and queries to their handlers and returns a uniform <see cref="Result"/> outcome.
/// </summary>
/// <remarks>
/// This is the single entry point callers use to execute application requests, decoupling them from concrete handler
/// types. Only the contract lives here; the DI-based implementation resides in <c>BuildingBlocks.Infrastructure</c>,
/// where it resolves the matching handler and the ordered pipeline behaviors from the container. All dispatch methods
/// are asynchronous and cooperatively cancellable; there are no synchronous overloads.
/// </remarks>
public interface ISender
{
	/// <summary>
	/// Sends a command that yields no value on success to its handler.
	/// </summary>
	/// <param name="command">The command to dispatch.</param>
	/// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
	/// <returns>A task whose result is a <see cref="Result"/> indicating success or an expected failure.</returns>
	Task<Result> Send(ICommand command, CancellationToken cancellationToken);

	/// <summary>
	/// Sends a command that yields a value of type <typeparamref name="TResult"/> on success to its handler.
	/// </summary>
	/// <param name="command">The command to dispatch.</param>
	/// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
	/// <typeparam name="TResult">The type of the value produced by the command on success.</typeparam>
	/// <returns>A task whose result is a <see cref="Result{TResult}"/> carrying the produced value on success, or an expected failure otherwise.</returns>
	Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);

	/// <summary>
	/// Sends a query to its handler.
	/// </summary>
	/// <param name="query">The query to dispatch.</param>
	/// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
	/// <typeparam name="TResult">The type of the value produced by the query.</typeparam>
	/// <returns>A task whose result is a <see cref="Result{TResult}"/> carrying the requested value on success, or an expected failure otherwise.</returns>
	Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
