namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Records which Wolverine defaults the host's Building Block selection requires.
/// </summary>
/// <remarks>
/// Populated by the <c>Use*</c> methods of <c>BuildingBlocksOptions</c> at composition time and consumed by
/// <see cref="BuildingBlocksWolverineExtension"/> when Wolverine bootstraps — the single source of truth that keeps
/// the capability selection and the Wolverine configuration in lockstep, so a host can no longer forget or
/// mismatch an <c>Apply*</c> call.
/// </remarks>
internal sealed class WolverineWiringSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the domain-event envelope routing is applied.
    /// </summary>
    /// <value><c>true</c> if a persistence style was selected and domain events flow through the outbox; otherwise, <c>false</c>.</value>
    public bool ApplyDomainEventRouting { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Wolverine's EF Core transactional middleware is applied.
    /// </summary>
    /// <value><c>true</c> if EF Core persistence was selected; otherwise, <c>false</c>.</value>
    public bool ApplyEfCoreOutbox { get; set; }

    /// <summary>
    /// Gets or sets the AMQP connection URI of the RabbitMQ broker.
    /// </summary>
    /// <value>The broker URI when Wolverine messaging was selected; otherwise, <see langword="null"/>.</value>
    public Uri? RabbitMqUri { get; set; }

    /// <summary>
    /// Gets a value indicating whether the host's selection requires a running Wolverine runtime.
    /// </summary>
    /// <value><c>true</c> if any capability that flows through Wolverine was selected; otherwise, <c>false</c>.</value>
    public bool RequiresWolverine => ApplyDomainEventRouting || ApplyEfCoreOutbox || RabbitMqUri is not null;
}
