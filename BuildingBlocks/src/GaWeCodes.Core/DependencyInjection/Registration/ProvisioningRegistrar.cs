using GaWeCodes.Core.DependencyInjection.Wiring;

namespace GaWeCodes.Core.DependencyInjection.Registration;

internal sealed class ProvisioningRegistrar(ProvisioningSelection provisioning)
{
    public void Select(InfrastructureProvisioning mode) => provisioning.Select(mode);
}
