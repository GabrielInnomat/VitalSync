namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

public enum StartupPhase
{
    BeforeHostedServicesStart = 0,
    AfterHostedServicesStarted,
}
