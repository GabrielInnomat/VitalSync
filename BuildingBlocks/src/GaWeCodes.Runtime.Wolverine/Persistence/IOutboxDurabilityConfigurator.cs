using Wolverine;

namespace GaWeCodes.Persistence;

public interface IOutboxDurabilityConfigurator
{
    void Configure(WolverineOptions options);
}
