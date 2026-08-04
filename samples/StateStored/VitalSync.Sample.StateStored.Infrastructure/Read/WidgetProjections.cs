using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Read;

public sealed class WidgetCreatedProjection(WidgetReadDbContext context) : IProjectionHandler<WidgetCreated>
{
    public async Task HandleAsync(WidgetCreated domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

        var existing = await context.Widgets
            .FirstOrDefaultAsync(widget => widget.Id == domainEvent.WidgetId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            context.Widgets.Add(new WidgetReadModel
            {
                Id = domainEvent.WidgetId,
                Name = domainEvent.Name,
                RenameCount = 0,
                Version = metadata.Version,
            });
        }
        else if (existing.Version < metadata.Version)
        {
            existing.Name = domainEvent.Name;
            existing.Version = metadata.Version;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class WidgetPartAddedProjection(WidgetReadDbContext context) : IProjectionHandler<WidgetPartAdded>
{
    public Task HandleAsync(WidgetPartAdded domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return WidgetPartProjection.ApplyAsync(
            context,
            domainEvent.WidgetId,
            metadata,
            widget =>
            {
                widget.PartCount++;
                widget.TotalQuantity += domainEvent.Quantity;
            },
            cancellationToken);
    }
}

public sealed class WidgetPartQuantityChangedProjection(WidgetReadDbContext context)
    : IProjectionHandler<WidgetPartQuantityChanged>
{
    public Task HandleAsync(
        WidgetPartQuantityChanged domainEvent,
        DomainEventMetadata metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return WidgetPartProjection.ApplyAsync(
            context,
            domainEvent.WidgetId,
            metadata,
            widget => widget.TotalQuantity += domainEvent.Quantity - domainEvent.PreviousQuantity,
            cancellationToken);
    }
}

public sealed class WidgetPartRemovedProjection(WidgetReadDbContext context) : IProjectionHandler<WidgetPartRemoved>
{
    public Task HandleAsync(WidgetPartRemoved domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return WidgetPartProjection.ApplyAsync(
            context,
            domainEvent.WidgetId,
            metadata,
            widget =>
            {
                widget.PartCount--;
                widget.TotalQuantity -= domainEvent.Quantity;
            },
            cancellationToken);
    }
}

internal static class WidgetPartProjection
{
    public static async Task ApplyAsync(
        WidgetReadDbContext context,
        WidgetId widgetId,
        DomainEventMetadata metadata,
        Action<WidgetReadModel> change,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var existing = await context.Widgets
            .FirstOrDefaultAsync(widget => widget.Id == widgetId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            existing = new WidgetReadModel { Id = widgetId, Version = metadata.Version };
            change(existing);
            context.Widgets.Add(existing);
        }
        else if (existing.Version < metadata.Version)
        {
            change(existing);
            existing.Version = metadata.Version;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class WidgetRenamedProjection(WidgetReadDbContext context) : IProjectionHandler<WidgetRenamed>
{
    public async Task HandleAsync(WidgetRenamed domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

        var existing = await context.Widgets
            .FirstOrDefaultAsync(widget => widget.Id == domainEvent.WidgetId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            context.Widgets.Add(new WidgetReadModel
            {
                Id = domainEvent.WidgetId,
                Name = domainEvent.Name,
                RenameCount = domainEvent.RenameCount,
                Version = metadata.Version,
            });
        }
        else if (existing.Version < metadata.Version)
        {
            existing.Name = domainEvent.Name;
            existing.RenameCount = domainEvent.RenameCount;
            existing.Version = metadata.Version;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
