namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Computes the retry backoff applied to failed outbox dispatch attempts.
/// </summary>
/// <remarks>
/// The backoff grows exponentially with the attempt count and is capped, so a poison message cannot hot-loop the drain
/// while healthy messages of other streams keep flowing.
/// </remarks>
internal static class OutboxRetryPolicy
{
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    public static TimeSpan GetBackoff(int attempts)
    {
        var exponent = Math.Min(Math.Max(attempts - 1, 0), 16);
        var delay = TimeSpan.FromTicks(BaseDelay.Ticks * (1L << exponent));
        return delay > MaxDelay ? MaxDelay : delay;
    }
}
