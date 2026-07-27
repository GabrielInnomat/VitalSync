using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Background service that drains the transactional outbox — the drain loop of the outbox-backed publisher.
/// </summary>
/// <remarks>
/// The processor wakes up whenever a unit of work signals a commit via <see cref="OutboxSignal"/> (immediate,
/// low-latency drain in the happy path) and additionally on a fixed poll interval as a crash-recovery safety net, so
/// messages committed just before a crash are still dispatched after restart. Each pass runs in its own service scope
/// and repeats until no pending messages remain. Hosts without a configured persistence style simply idle, because no
/// <see cref="OutboxDrainer"/> is registered.
/// </remarks>
/// <param name="serviceProvider">The root service provider used to create a scope per drain pass.</param>
/// <param name="signal">The wake-up signal notified by committing units of work.</param>
/// <param name="logger">The logger for drain-loop failures.</param>
public sealed class OutboxProcessor(IServiceProvider serviceProvider, OutboxSignal signal, ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private static readonly Action<ILogger, Exception> DrainFailedMessage =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, "OutboxDrainFailed"),
            "Draining the outbox failed; the next pass will retry");

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await signal.WaitAsync(PollInterval, stoppingToken).ConfigureAwait(false);
                await using var scope = serviceProvider.CreateAsyncScope();
                var drainer = scope.ServiceProvider.GetService<OutboxDrainer>();
                if (drainer is null)
                {
                    continue;
                }

                while (await drainer.DrainAsync(stoppingToken).ConfigureAwait(false) > 0)
                {
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // The drain loop must survive any failure; the next pass retries.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                DrainFailedMessage(logger, exception);
            }
        }
    }
}
