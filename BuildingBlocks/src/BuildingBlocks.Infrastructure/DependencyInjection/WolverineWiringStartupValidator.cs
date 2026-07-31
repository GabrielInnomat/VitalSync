using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.Runtime;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// Hosted service that fails the host at startup when the selected Building Block capabilities require Wolverine
/// but the host never called <c>UseWolverine</c>.
/// </summary>
/// <remarks>
/// The persistence styles and the messaging transport all flow through Wolverine's transactional outbox
/// (ADR-0022/0023), yet <c>UseWolverine</c> lives on the host builder and cannot be registered from an
/// <c>IServiceCollection</c> extension. Without this check a forgotten <c>UseWolverine</c> surfaces only when the
/// first commit tries to resolve the outbox — in production. The validator instead probes for Wolverine's runtime
/// registration when the host starts and fails fast with an actionable message (ADR-0027). Hosts opt out via
/// <see cref="BuildingBlocksOptions.ValidateWolverineOnStart"/>.
/// </remarks>
/// <param name="serviceProvider">The root service provider probed for the Wolverine runtime registration.</param>
internal sealed class WolverineWiringStartupValidator(IServiceProvider serviceProvider) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Validate();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void Validate()
    {
        if (serviceProvider.GetService<IWolverineRuntime>() is null)
        {
            throw new InvalidOperationException(
                "The selected Building Block capabilities (persistence and/or integration-event messaging) require " +
                "Wolverine, but no Wolverine runtime is registered. Call UseWolverine() on the host builder — the " +
                "Building Blocks apply their own Wolverine configuration automatically via a registered " +
                "IWolverineExtension, so the call needs no Building Block-specific setup. To run without this " +
                "check, set BuildingBlocksOptions.ValidateWolverineOnStart to false.");
        }
    }
}
