using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Infrastructure.Persistence.EventSourced;

internal sealed record TrackedAggregate(
    IDomainEventOwner Aggregate,
    string AggregateName,
    string AggregateId,
    Func<long> Version) : ITrackedAggregate
{
    public long CurrentVersion => Version();
}
