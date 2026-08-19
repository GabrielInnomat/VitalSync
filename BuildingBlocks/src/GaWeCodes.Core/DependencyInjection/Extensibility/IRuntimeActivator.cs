using Microsoft.Extensions.Hosting;

namespace GaWeCodes.Core.DependencyInjection.Extensibility;

public interface IRuntimeActivator
{
    void Activate(IHostApplicationBuilder builder, IWiringSnapshot wiring);
}
