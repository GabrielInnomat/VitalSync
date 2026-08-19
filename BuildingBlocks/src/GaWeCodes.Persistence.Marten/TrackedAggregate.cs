using GaWeCodes.Core.Persistence;
using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;

namespace GaWeCodes.Persistence.Marten;

internal sealed record TrackedAggregate(
    IDomainEventOwner Aggregate,
    string AggregateName,
    string AggregateId,
    Func<long> Version) : ITrackedAggregate
{
    public long CurrentVersion => Version();
}
