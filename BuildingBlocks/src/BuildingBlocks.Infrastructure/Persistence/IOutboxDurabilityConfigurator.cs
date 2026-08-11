using Wolverine;

namespace BuildingBlocks.Infrastructure.Persistence;

internal interface IOutboxDurabilityConfigurator
{
    void Configure(WolverineOptions options);
}
