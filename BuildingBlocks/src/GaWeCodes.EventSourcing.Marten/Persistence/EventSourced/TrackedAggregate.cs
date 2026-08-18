using GaWeCodes.Domain.Events;

namespace GaWeCodes.Persistence.EventSourced;

internal sealed record TrackedAggregate(
    IDomainEventOwner Aggregate,
    string AggregateName,
    string AggregateId,
    Func<long> Version) : ITrackedAggregate
{
    public long CurrentVersion => Version();
}
