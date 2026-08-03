using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed generic repository for state-stored aggregates.
/// </summary>
/// <remarks>
/// The repository maps the aggregate's <b>state</b> object, not the aggregate: the aggregate is behavior with an
/// <c>Id</c> derived from its state (ADR-0008/0010/0025), which EF Core cannot use as a primary key because it has
/// neither setter nor backing field. The state, by contrast, is an immutable record whose identity is a plain mapped
/// property, so it maps as an ordinary entity type — one table, one identity column, no shadow key. Loading therefore
/// fetches the state and rehydrates an empty aggregate around it (see <see cref="IStateOwner"/>); the aggregate is
/// registered with the <see cref="EfCoreAggregateTracker"/>, which the unit of work consults at commit because EF's
/// change tracker only ever sees the state.
/// <para>
/// The repository works against the bounded context's write database only (ADR-0021) and deliberately offers no query
/// surface beyond <see cref="GetByIdAsync"/> — queries read the context's read database directly. There is no
/// <c>Update</c> method (changes flow through the <see cref="IUnitOfWork"/>) and no <c>Remove</c> method (removal is
/// modeled as a soft-delete state change in the domain). Register it open-generically via <c>UseEfCorePersistence</c>
/// so each aggregate type resolves the same implementation.
/// </para>
/// </remarks>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
/// <typeparam name="TKey">The type of the aggregate root's identity key.</typeparam>
/// <param name="context">The write-database context the repository operates on.</param>
/// <param name="tracker">The tracker recording which aggregates take part in the current unit of work.</param>
public sealed class EfCoreRepository<TAggregate, TKey>(DbContext context, EfCoreAggregateTracker tracker)
    : IRepository<TAggregate, TKey>
    where TAggregate : class, IAggregateRoot<TKey>, IReconstitutable<TAggregate>
    where TKey : struct, IEntityKey
{
    /// <inheritdoc/>
    public async Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken)
    {
        var aggregate = CreateEmpty(out var stateOwner);

        var state = await context
            .FindAsync(stateOwner.StateType, [id], cancellationToken)
            .ConfigureAwait(false);

        if (state is null)
        {
            return null;
        }

        stateOwner.Restore(state);
        tracker.Track((IDomainEventOwner)aggregate, stateOwner, state);
        return aggregate;
    }

    /// <inheritdoc/>
    public Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var stateOwner = AsStateOwner(aggregate);
        var state = stateOwner.State;

        context.Add(state);
        tracker.Track((IDomainEventOwner)aggregate, stateOwner, state);
        return Task.CompletedTask;
    }

    // The aggregate is constructed before the lookup so its state type is available without reflecting over
    // TAggregate. Construction goes through the aggregate's own explicit IReconstitutable implementation, so no
    // public constructor is demanded and nothing is resolved by reflection (ADR-0025 reconstitution amendment).
    private static TAggregate CreateEmpty(out IStateOwner stateOwner)
    {
        var aggregate = TAggregate.CreateEmpty();

        stateOwner = AsStateOwner(aggregate);
        return aggregate;
    }

    private static IStateOwner AsStateOwner(TAggregate aggregate) =>
        aggregate as IStateOwner
        ?? throw new InvalidOperationException(
            $"The aggregate '{typeof(TAggregate)}' does not expose its state and cannot be persisted by EF Core.");
}
