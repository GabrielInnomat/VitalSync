using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Domain.Events;

namespace GaWeCodes.Persistence.StateStored;

internal sealed record TrackedStateAggregate(
    IDomainEventOwner Aggregate,
    IStateOwner StateOwner,
    object PersistedState,
    string AggregateName,
    string AggregateId) : ITrackedAggregate
{
    public long CurrentVersion => StateOwner.Version;
}
