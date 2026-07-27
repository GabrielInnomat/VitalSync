using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// EF Core model configuration for the transactional outbox.
/// </summary>
/// <remarks>
/// State-stored contexts must map the outbox into their <b>write-database</b> model so outbox writes share the
/// command's transaction (ADR-0022): call <see cref="AddOutboxMessages"/> from the context's
/// <c>OnModelCreating</c>. Event-sourced contexts do not use this — their outbox messages are Marten documents.
/// </remarks>
public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// Maps the <see cref="OutboxMessage"/> entity into the write-database model.
    /// </summary>
    /// <param name="modelBuilder">The model builder of the write-database context.</param>
    /// <returns>The same model builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
    public static ModelBuilder AddOutboxMessages(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entity = modelBuilder.Entity<OutboxMessage>();
        entity.ToTable("outbox_messages");
        entity.HasKey(message => message.Id);
        entity.Property(message => message.Id).ValueGeneratedOnAdd();
        entity.Property(message => message.StreamId).HasMaxLength(512);
        entity.Property(message => message.EventType).HasMaxLength(1024);
        entity.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt, message.Id });

        return modelBuilder;
    }
}
