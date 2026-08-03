using BuildingBlocks.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

internal sealed partial class MissingUnitOfWorkStartupLogger : IHostedService
{
    private readonly ILogger<MissingUnitOfWorkStartupLogger> _logger;

    public MissingUnitOfWorkStartupLogger(ILogger<MissingUnitOfWorkStartupLogger> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogNoPersistenceConfigured(_logger);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No persistence configured — commands are dispatched without a unit of work and nothing is committed. This is intended only for tests, gateway services, and hosts with their own persistence.")]
    private static partial void LogNoPersistenceConfigured(ILogger logger);
}
