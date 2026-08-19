using GaWeCodes.Thessera.Core.DependencyInjection;
using GaWeCodes.Thessera.Core.DependencyInjection.Extensibility;
using GaWeCodes.Thessera.Core.DependencyInjection.Registration;
using GaWeCodes.Thessera.Core.DependencyInjection.Wiring;
using GaWeCodes.Thessera.Core.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GaWeCodes.Thessera.Tests;

public sealed class PersistenceAdapterTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;******";

    [Fact]
    public void TheRegistrarLetsTheAdapterRegisterItsOwnServices()
    {
        var services = new ServiceCollection();
        var persistence = new PersistenceSelection();
        var adapter = new RecordingAdapter(ConnectionString);

        new PersistenceRegistrar(services, persistence, new ProvisioningSelection(), new RuntimeActivation())
            .Use(adapter);

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

        new PersistenceRegistrar(new ServiceCollection(), new PersistenceSelection(), provisioning, new RuntimeActivation())
            .Use(adapter);

        Assert.False(adapter.SeenContext!.ProvisionsInfrastructure);

        provisioning.Select(InfrastructureProvisioning.AtStartup);

        Assert.True(adapter.SeenContext.ProvisionsInfrastructure);
    }

    [Fact]
    public void TheRuntimeContributedByTheAdapter_ReachesTheWiring()
    {
        var runtime = new RuntimeActivation();

        new PersistenceRegistrar(new ServiceCollection(), new PersistenceSelection(), new ProvisioningSelection(), runtime)
            .Use(new RecordingAdapter(ConnectionString) { ContributesRuntime = true });

        Assert.IsType<RecordingActivator>(runtime.Activator);
    }

    [Fact]
    public void TwoAdaptersAskingForTheSameRuntime_ShareOneActivator()
    {
        var runtime = new RuntimeActivation();

        var first = runtime.GetOrAdd(static () => new RecordingActivator());
        var second = runtime.GetOrAdd(static () => new RecordingActivator());

        Assert.Same(first, second);
    }

    [Fact]
    public void TwoDifferentRuntimes_FailWithAnExplanation()
    {
        var runtime = new RuntimeActivation();
        runtime.GetOrAdd(static () => new RecordingActivator());

        var thrown = Assert.Throws<InvalidOperationException>(
            () => runtime.GetOrAdd(static () => new OtherActivator()));

        Assert.Contains("exactly one messaging runtime", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheTransientFaultDecision_ComesFromTheChosenAdapter()
    {
        var persistence = new PersistenceSelection();
        var transient = new TimeoutException();

        Assert.False(persistence.IsTransientFault(transient));

        new PersistenceRegistrar(new ServiceCollection(), persistence, new ProvisioningSelection(), new RuntimeActivation())
            .Use(new RecordingAdapter(ConnectionString) { TransientFault = transient });

        Assert.True(persistence.IsTransientFault(transient));
        Assert.False(persistence.IsTransientFault(new InvalidOperationException()));
    }

    private sealed record RecordingAdapter(string WriteConnectionString) : IPersistenceAdapter
    {
        public string Description => "UseRecordingPersistence";

        public AggregateStyle AggregateStyle => AggregateStyle.StateStored;

        public bool ContributesRuntime { get; init; }

        public Exception? TransientFault { get; init; }

        public bool WasRegistered { get; private set; }

        public IServiceCollection? SeenServices { get; private set; }

        public PersistenceRegistrationContext? SeenContext { get; private set; }

        public bool IsTransientFault(Exception exception) =>
            TransientFault is not null && ReferenceEquals(TransientFault, exception);

        public void Register(PersistenceRegistrationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            WasRegistered = true;
            SeenServices = context.Services;
            SeenContext = context;

            if (ContributesRuntime)
            {
                context.UseRuntime(static () => new RecordingActivator());
            }
        }
    }

    private sealed class RecordingActivator : IRuntimeActivator
    {
        public void Activate(IHostApplicationBuilder builder, IWiringSnapshot wiring)
        {
        }
    }

    private sealed class OtherActivator : IRuntimeActivator
    {
        public void Activate(IHostApplicationBuilder builder, IWiringSnapshot wiring)
        {
        }
    }
}
