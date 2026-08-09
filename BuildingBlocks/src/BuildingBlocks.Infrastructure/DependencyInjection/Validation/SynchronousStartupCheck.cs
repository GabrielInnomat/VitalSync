namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal abstract class SynchronousStartupCheck : IStartupCheck
{
    public abstract StartupPhase Phase { get; }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        Run();
        return Task.CompletedTask;
    }

    protected abstract void Run();
}
