namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal interface IStartupCheck
{
    StartupPhase Phase { get; }

    void Run();
}
