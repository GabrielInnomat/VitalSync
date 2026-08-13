using BuildingBlocks.Infrastructure.DependencyInjection.Extensibility;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

public sealed class WolverineRuntimeActivator : IRuntimeActivator
{
    private readonly List<IOutboxDurabilityConfigurator> _outboxDurability = [];
    private readonly List<Action<WolverineOptions>> _customizations = [];

    public void AddOutboxDurability(IOutboxDurabilityConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _outboxDurability.Add(configurator);
    }

    public void Customize(Action<WolverineOptions> customize)
    {
        ArgumentNullException.ThrowIfNull(customize);

        _customizations.Add(customize);
    }

    public void Activate(IHostApplicationBuilder builder, IWiringSnapshot wiring)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(wiring);

        builder.UseWolverine(options =>
        {
            foreach (var durability in _outboxDurability)
            {
                durability.Configure(options);
            }

            foreach (var customize in _customizations)
            {
                customize(options);
            }
        });
    }
}
