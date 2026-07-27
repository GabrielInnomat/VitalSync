using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Represents a repository for state-stored aggregates whose current state is persisted directly.
/// </summary>
/// <remarks>
/// The repository operates against the bounded context's write database only; queries never go through repositories,
/// because the read side reads its own read database directly. There is deliberately no <c>Update</c> method:
/// aggregates are tracked after retrieval and their changes flow through the <see cref="IUnitOfWork"/> at commit
/// time. Likewise there are no query methods beyond <see cref="GetByIdAsync"/>. Only the contract lives here; the
/// EF Core-backed implementation resides in <c>BuildingBlocks.Infrastructure</c>.
/// </remarks>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
/// <typeparam name="TKey">The type of the aggregate root's identity key.</typeparam>
public interface IRepository<TAggregate, in TKey>
    where TAggregate : AggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Retrieves the aggregate with the specified identifier from the write database.
    /// </summary>
    /// <param name="id">The unique identifier of the aggregate to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task whose result is the aggregate, or <see langword="null"/> when no aggregate with the specified identifier exists.</returns>
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new aggregate to the repository.
    /// </summary>
    /// <remarks>
    /// The aggregate is persisted when the surrounding <see cref="IUnitOfWork"/> commits.
    /// </remarks>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken);

    /// <summary>
    /// Marks an aggregate for removal from the repository.
    /// </summary>
    /// <remarks>
    /// The removal is persisted when the surrounding <see cref="IUnitOfWork"/> commits.
    /// </remarks>
    /// <param name="aggregate">The aggregate to remove.</param>
    void Remove(TAggregate aggregate);
}
