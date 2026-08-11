namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

public interface IStartupCheck
{
    StartupPhase Phase { get; }

    Task RunAsync(CancellationToken cancellationToken);
}
