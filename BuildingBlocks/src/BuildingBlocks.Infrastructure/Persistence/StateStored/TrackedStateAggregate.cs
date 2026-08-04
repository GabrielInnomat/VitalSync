using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence;

namespace BuildingBlocks.Infrastructure.Persistence.StateStored;

internal sealed record TrackedStateAggregate(
    IDomainEventOwner Aggregate,
    IStateOwner StateOwner,
    object PersistedState,
    string AggregateName,
    string AggregateId) : ITrackedAggregate
{
    public long CurrentVersion => StateOwner.Version;
}
