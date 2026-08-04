namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal enum StartupPhase
{
    BeforeHostedServicesStart = 0,
    AfterHostedServicesStarted,
}
