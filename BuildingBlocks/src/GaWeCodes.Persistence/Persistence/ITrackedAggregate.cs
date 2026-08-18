using GaWeCodes.Domain.Events;

namespace GaWeCodes.Persistence;

public interface ITrackedAggregate
{
    IDomainEventOwner Aggregate { get; }

    string AggregateName { get; }

    string AggregateId { get; }

    long CurrentVersion { get; }
}
