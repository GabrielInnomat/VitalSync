using BuildingBlocks.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// Hosted service that logs once at host startup when no <see cref="IUnitOfWork"/> is registered.
/// </summary>
/// <remarks>
/// Without a registered unit of work, commands pass through <c>UnitOfWorkBehavior</c> without a commit — intended
/// for handler tests, gateway/facade services, and services with their own persistence, but almost always a
/// configuration error in a service host that owns state. A single <see cref="LogLevel.Information"/> entry at
/// startup makes the silent no-op visible instead of leaving uncommitted commands to be discovered as missing data.
/// It is registered by <see cref="ServiceCollectionExtensions.AddBuildingBlocks"/> only when the service collection
/// contains no <see cref="IUnitOfWork"/> at the end of the call.
/// </remarks>
internal sealed partial class MissingUnitOfWorkStartupLogger : IHostedService
{
    private readonly ILogger<MissingUnitOfWorkStartupLogger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingUnitOfWorkStartupLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger used to emit the startup notice.</param>
    public MissingUnitOfWorkStartupLogger(ILogger<MissingUnitOfWorkStartupLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the missing-persistence notice.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogNoPersistenceConfigured(_logger);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Does nothing; the notice is only emitted at startup.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No persistence configured — commands are dispatched without a unit of work and nothing is committed. This is intended only for tests, gateway services, and hosts with their own persistence.")]
    private static partial void LogNoPersistenceConfigured(ILogger logger);
}
