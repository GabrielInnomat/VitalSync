namespace BuildingBlocks.Application;

/// <summary>
/// Handles a query of type <typeparamref name="TQuery"/> and produces a value of type <typeparamref name="TResult"/>.
/// </summary>
/// <remarks>
/// Implement one handler per query type; it reads domain state without mutating it and returns a
/// <see cref="Result{TResult}"/> carrying the requested data on success or an expected failure such as
/// <see cref="ErrorCategory.NotFound"/> otherwise. Handlers are asynchronous and cooperatively cancellable; there are no
/// synchronous overloads.
/// </remarks>
/// <typeparam name="TQuery">The type of query handled by this handler.</typeparam>
/// <typeparam name="TResult">The type of the value produced by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
	where TQuery : IQuery<TResult>
{
	/// <summary>
	/// Handles the specified query.
	/// </summary>
	/// <param name="query">The query to handle.</param>
	/// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
	/// <returns>A task whose result is a <see cref="Result{TResult}"/> carrying the requested value on success, or an expected failure otherwise.</returns>
	Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
}
