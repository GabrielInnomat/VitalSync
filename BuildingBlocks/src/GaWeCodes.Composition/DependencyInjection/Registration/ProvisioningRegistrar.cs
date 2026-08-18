using GaWeCodes.DependencyInjection.Wiring;

namespace GaWeCodes.DependencyInjection.Registration;

internal sealed class ProvisioningRegistrar(ProvisioningSelection provisioning)
{
    public void Select(InfrastructureProvisioning mode) => provisioning.Select(mode);
}
