using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Extensibility;

public interface IRuntimeActivator
{
    void Activate(IHostApplicationBuilder builder, IWiringSnapshot wiring);
}
