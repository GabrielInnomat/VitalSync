using Wolverine;

namespace BuildingBlocks.Infrastructure.Persistence;

public interface IOutboxDurabilityConfigurator
{
    void Configure(WolverineOptions options);
}
