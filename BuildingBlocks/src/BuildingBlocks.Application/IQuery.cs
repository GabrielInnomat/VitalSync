namespace BuildingBlocks.Application;

/// <summary>
/// Represents a read-only request that returns a value of type <typeparamref name="TResult"/> and never mutates state.
/// </summary>
/// <remarks>
/// Implement this marker on request types that read domain state without changing it, keeping the read side clearly
/// separated from commands per CQRS. Each query is handled by exactly one <see cref="IQueryHandler{TQuery, TResult}"/>
/// and is dispatched through <see cref="ISender"/>, which returns a <see cref="Result{TResult}"/>.
/// </remarks>
/// <typeparam name="TResult">The type of the value produced by the query.</typeparam>
public interface IQuery<TResult>
{
}
