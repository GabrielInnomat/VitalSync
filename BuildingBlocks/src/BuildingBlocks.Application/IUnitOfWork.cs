namespace BuildingBlocks.Application;

/// <summary>
/// Represents a unit of work that atomically commits all changes made while handling a single command.
/// </summary>
/// <remarks>
/// Exactly one unit of work spans each command dispatch; it is owned by the unit-of-work pipeline behavior in
/// <c>BuildingBlocks.Infrastructure</c>, so command handlers never commit themselves. On commit, within a single
/// write-database transaction, the implementation persists the aggregate changes, collects the aggregates'
/// uncommitted domain events, writes those events to the transactional outbox atomically with the state change, and
/// clears the aggregates' event collections. Only the contract lives here; the persistence-specific implementations
/// reside in <c>BuildingBlocks.Infrastructure</c>.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all changes made within the current unit of work as a single atomic transaction.
    /// </summary>
    /// <remarks>
    /// Committing persists the aggregate changes, writes the aggregates' uncommitted domain events to the
    /// transactional outbox within the same transaction, and clears the aggregates' event collections. If the commit
    /// fails, no state change and no outbox entry is observed.
    /// </remarks>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous commit operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken);
}
