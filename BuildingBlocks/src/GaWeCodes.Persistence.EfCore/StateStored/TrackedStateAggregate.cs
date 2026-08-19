using GaWeCodes.Core.Persistence;
using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;

namespace GaWeCodes.Persistence.EfCore.StateStored;

internal sealed record TrackedStateAggregate(
    IDomainEventOwner Aggregate,
    IStateOwner StateOwner,
    object PersistedState,
    string AggregateName,
    string AggregateId) : ITrackedAggregate
{
    public long CurrentVersion => StateOwner.Version;
}
