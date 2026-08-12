using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Registration;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class PersistenceAdapterTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;******";

    [Fact]
    public void TheRegistrarLetsTheAdapterRegisterItsOwnServices()
    {
        var services = new ServiceCollection();
        var persistence = new PersistenceSelection();
        var adapter = new RecordingAdapter(ConnectionString);

        new PersistenceRegistrar(services, persistence, new ProvisioningSelection()).Use(adapter);

        Assert.True(adapter.WasRegistered);
        Assert.Same(services, adapter.SeenServices);
        Assert.True(persistence.IsSelected);
        Assert.Same(adapter, persistence.Choice.Adapter);
    }

    [Fact]
    public void TheAdapterSeesTheProvisioningDecisionOfTheHost()
    {
        var provisioning = new ProvisioningSelection();
        var adapter = new RecordingAdapter(ConnectionString);

        new PersistenceRegistrar(new ServiceCollection(), new PersistenceSelection(), provisioning)
            .Use(adapter);

        Assert.False(adapter.SeenContext!.ProvisionsInfrastructure);

        provisioning.Select(InfrastructureProvisioning.AtStartup);

        Assert.True(adapter.SeenContext.ProvisionsInfrastructure);
    }

    [Fact]
    public void OutboxDurabilityContributedByTheAdapter_ReachesTheWiring()
    {
        var persistence = new PersistenceSelection();
        var configurator = new RecordingOutboxDurability();

        new PersistenceRegistrar(new ServiceCollection(), persistence, new ProvisioningSelection())
            .Use(new RecordingAdapter(ConnectionString) { OutboxDurability = configurator });

        Assert.Same(configurator, Assert.Single(persistence.OutboxDurability));
    }

    private sealed record RecordingAdapter(string WriteConnectionString) : IPersistenceAdapter
    {
        public string Description => "UseRecordingPersistence";

        public IOutboxDurabilityConfigurator? OutboxDurability { get; init; }

        public bool WasRegistered { get; private set; }

        public IServiceCollection? SeenServices { get; private set; }

        public PersistenceRegistrationContext? SeenContext { get; private set; }

        public void Register(PersistenceRegistrationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            WasRegistered = true;
            SeenServices = context.Services;
            SeenContext = context;

            if (OutboxDurability is not null)
            {
                context.AddOutboxDurability(OutboxDurability);
            }
        }
    }

    private sealed class RecordingOutboxDurability : IOutboxDurabilityConfigurator
    {
        public void Configure(WolverineOptions options)
        {
        }
    }
}
