using Microsoft.Extensions.Hosting;

namespace GaWeCodes.DependencyInjection.Extensibility;

public interface IRuntimeActivator
{
    void Activate(IHostApplicationBuilder builder, IWiringSnapshot wiring);
}
