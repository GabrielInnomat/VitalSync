using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence;

namespace BuildingBlocks.Infrastructure.Persistence.StateStored;

internal sealed class EfCoreAggregateTracker : AggregateTracker<TrackedStateAggregate>
{
    public void Track(
        IDomainEventOwner aggregate,
        IStateOwner stateOwner,
        object persistedState,
        string aggregateName,
        string aggregateId)
    {
        ArgumentNullException.ThrowIfNull(stateOwner);
        ArgumentNullException.ThrowIfNull(persistedState);

        Add(new TrackedStateAggregate(aggregate, stateOwner, persistedState, aggregateName, aggregateId));
    }
}
