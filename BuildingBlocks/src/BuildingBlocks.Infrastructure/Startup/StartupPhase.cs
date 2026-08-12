namespace BuildingBlocks.Infrastructure.Startup;

public enum StartupPhase
{
    BeforeHostedServicesStart = 0,
    AfterHostedServicesStarted,
}
