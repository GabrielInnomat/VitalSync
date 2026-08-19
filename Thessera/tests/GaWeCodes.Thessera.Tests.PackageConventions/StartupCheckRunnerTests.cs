using GaWeCodes.Thessera.Core.DependencyInjection;
using GaWeCodes.Thessera.Core.Startup;
using GaWeCodes.Thessera.Persistence.Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GaWeCodes.Thessera.Tests;

public sealed class StartupCheckRunnerTests
{
    [Fact]
    public async Task ABeforeCheck_RunsWhileHostedServicesAreStarting()
    {
        var check = new RecordingCheck(StartupPhase.BeforeHostedServicesStart);
        var runner = CreateRunner(check);

        await runner.StartAsync(CancellationToken.None);

        Assert.Equal(1, check.Runs);

        await runner.StartedAsync(CancellationToken.None);

        Assert.Equal(1, check.Runs);
    }

    [Fact]
    public async Task AnAfterCheck_RunsOnlyOnceEveryHostedServiceHasStarted()
    {
        var check = new RecordingCheck(StartupPhase.AfterHostedServicesStarted);
        var runner = CreateRunner(check);

        await runner.StartAsync(CancellationToken.None);

        Assert.Equal(0, check.Runs);

        await runner.StartedAsync(CancellationToken.None);

        Assert.Equal(1, check.Runs);
    }

    [Fact]
    public async Task AFailingCheck_StopsTheStartupImmediately()
    {
        var first = new ThrowingCheck();
        var second = new RecordingCheck(StartupPhase.BeforeHostedServicesStart);
        var runner = CreateRunner(first, second);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.StartAsync(CancellationToken.None));

        Assert.Equal(0, second.Runs);
    }

    [Fact]
    public async Task TheAfterPhase_RunsAfterTheStartOfAServiceRegisteredLater()
    {
        var observed = new List<string>();

        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IStartupCheck>(
                    new CallbackCheck(StartupPhase.AfterHostedServicesStarted, () => observed.Add("check")));
                services.AddHostedService<StartupCheckRunner>();
                services.AddHostedService(_ => new CallbackHostedService(() => observed.Add("late-service")));
            })
            .Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["late-service", "check"], observed);
    }

    private static readonly string[] ContributedByAPersistenceAdapter =
    [
        "AggregateStateModelCheck`1",
        "MartenSchemaProvisioner",
    ];

    [Fact]
    public void EveryStartupCheckInTheAssembly_IsReachableThroughTheRunner()
    {
        var implementations = typeof(ThesseraOptions).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => typeof(IStartupCheck).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThessera(_ => { });
        using var provider = services.BuildServiceProvider();

        var registered = provider.GetServices<IStartupCheck>()
            .Select(check => check.GetType().Name)
            .ToHashSet(StringComparer.Ordinal);

        var unregistered = implementations
            .Except(registered, StringComparer.Ordinal)
            .Except(ContributedByAPersistenceAdapter, StringComparer.Ordinal);

        Assert.Empty(unregistered);
    }

    [Fact]
    public void AChoosingPersistenceAdapter_ContributesItsOwnStartupCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThessera(options =>
        {
            options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
            options.UseMartenEventStore("Host=localhost;Database=test;Username=test;******");
        });

        using var provider = services.BuildServiceProvider();

        Assert.Contains(
            provider.GetServices<IStartupCheck>(),
            check => check is MartenSchemaProvisioner);
    }

    private static IHostedLifecycleService CreateRunner(params IStartupCheck[] checks) =>
        new StartupCheckRunner(checks);

    private sealed class RecordingCheck(StartupPhase phase) : SynchronousStartupCheck
    {
        public int Runs { get; private set; }

        public override StartupPhase Phase => phase;

        protected override void Run() => Runs++;
    }

    private sealed class CallbackCheck(StartupPhase phase, Action onRun) : SynchronousStartupCheck
    {
        public override StartupPhase Phase => phase;

        protected override void Run() => onRun();
    }

    private sealed class ThrowingCheck : SynchronousStartupCheck
    {
        public override StartupPhase Phase => StartupPhase.BeforeHostedServicesStart;

        protected override void Run() => throw new InvalidOperationException("boom");
    }

    private sealed class CallbackHostedService(Action onStart) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            onStart();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
