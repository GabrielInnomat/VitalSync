namespace BuildingBlocks.Infrastructure.Startup;

public interface IStartupCheck
{
    StartupPhase Phase { get; }

    Task RunAsync(CancellationToken cancellationToken);
}
