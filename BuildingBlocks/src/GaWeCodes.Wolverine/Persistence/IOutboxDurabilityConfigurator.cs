using Wolverine;

namespace GaWeCodes.Wolverine.Persistence;

public interface IOutboxDurabilityConfigurator
{
    void Configure(WolverineOptions options);
}
