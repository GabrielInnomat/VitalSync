using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;

namespace GaWeCodes.Core.Persistence;

public interface ITrackedAggregate
{
    IDomainEventOwner Aggregate { get; }

    string AggregateName { get; }

    string AggregateId { get; }

    long CurrentVersion { get; }
}
