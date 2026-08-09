namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal interface IStartupCheck
{
    StartupPhase Phase { get; }

    Task RunAsync(CancellationToken cancellationToken);
}
