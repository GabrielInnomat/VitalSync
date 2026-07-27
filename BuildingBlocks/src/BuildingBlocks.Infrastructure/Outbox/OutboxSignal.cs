namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Coalescing wake-up signal that lets a committing unit of work trigger an immediate outbox drain.
/// </summary>
/// <remarks>
/// The publisher drains the outbox immediately after a commit in the happy path (ADR-0022), keeping read-model lag
/// low: each successful commit calls <see cref="Notify"/>, and the drain loop awaits <see cref="WaitAsync"/> with a
/// poll-interval timeout as a safety net, so correctness never depends on the signal being observed. Multiple
/// notifications between two waits coalesce into one.
/// </remarks>
public sealed class OutboxSignal : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    /// <summary>
    /// Signals that new outbox messages have been committed and the drain loop should run.
    /// </summary>
    /// <remarks>
    /// Safe to call from any thread; redundant notifications while a signal is already pending are coalesced.
    /// </remarks>
    public void Notify()
    {
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // A wake-up is already pending; coalesce.
        }
    }

    /// <summary>
    /// Waits until the signal is set or the timeout elapses.
    /// </summary>
    /// <param name="timeout">The maximum time to wait before returning regardless of a signal.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task whose result is <c>true</c> if the signal was set; otherwise, <c>false</c>.</returns>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _semaphore.WaitAsync(timeout, cancellationToken);

    /// <inheritdoc/>
    public void Dispose() => _semaphore.Dispose();
}
