using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Read;

public sealed class WidgetCreatedProjection(WidgetReadDbContext context) : IProjectionHandler<WidgetCreated>
{
    public async Task Handle(WidgetCreated domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
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

public sealed class WidgetRenamedProjection(WidgetReadDbContext context) : IProjectionHandler<WidgetRenamed>
{
    public async Task Handle(WidgetRenamed domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
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
