using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Registration;

internal sealed class ProvisioningRegistrar(BuildingBlocksWiringSettings wiring)
{
    public void Select(InfrastructureProvisioning provisioning) => wiring.SelectProvisioning(provisioning);
}
