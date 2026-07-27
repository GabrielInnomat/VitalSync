using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Wolverine host configuration defaults for the Building Blocks messaging backbone.
/// </summary>
/// <remarks>
/// Service hosts call <see cref="ApplyBuildingBlockMessagingDefaults"/> from their <c>UseWolverine</c> setup to get the
/// RabbitMQ transport with sane, overridable defaults: auto-provisioned broker objects, a retry-with-cooldown policy
/// for transient consumer failures, and dead-lettering to the error queue once retries are exhausted (ADR-0023).
/// Anything configured afterwards on the same options overrides these defaults.
/// </remarks>
public static class WolverineOptionsExtensions
{
    /// <summary>
    /// Applies the default RabbitMQ transport, retry, and dead-letter configuration.
    /// </summary>
    /// <param name="options">The Wolverine options being configured by the host.</param>
    /// <param name="rabbitMqUri">The AMQP connection URI of the RabbitMQ broker (typically the Aspire-provided connection string).</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="rabbitMqUri"/> is <see langword="null"/>.</exception>
    public static WolverineOptions ApplyBuildingBlockMessagingDefaults(this WolverineOptions options, Uri rabbitMqUri)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(rabbitMqUri);

        options.UseRabbitMq(rabbitMqUri).AutoProvision();

        options.Policies.OnException<Exception>()
            .RetryWithCooldown(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(2))
            .Then.MoveToErrorQueue();

        return options;
    }
}
