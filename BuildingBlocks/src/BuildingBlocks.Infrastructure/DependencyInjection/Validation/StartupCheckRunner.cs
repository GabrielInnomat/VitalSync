using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed class StartupCheckRunner(IEnumerable<IStartupCheck> checks) : IHostedLifecycleService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Run(StartupPhase.BeforeHostedServicesStart);
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        Run(StartupPhase.AfterHostedServicesStarted);
        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void Run(StartupPhase phase)
    {
        foreach (var check in checks)
        {
            if (check.Phase == phase)
            {
                check.Run();
            }
        }
    }
}

