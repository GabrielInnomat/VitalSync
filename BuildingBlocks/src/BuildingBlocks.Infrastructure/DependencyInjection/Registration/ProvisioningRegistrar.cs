using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Registration;

internal sealed class ProvisioningRegistrar(ProvisioningSelection provisioning)
{
    public void Select(InfrastructureProvisioning mode) => provisioning.Select(mode);
}
