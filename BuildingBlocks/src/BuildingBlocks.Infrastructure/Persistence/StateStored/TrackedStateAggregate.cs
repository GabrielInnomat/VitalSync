using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence.StateStored;

internal sealed record TrackedStateAggregate(
    IDomainEventOwner Aggregate,
    IStateOwner StateOwner,
    object PersistedState,
    string AggregateName,
    string AggregateId);
