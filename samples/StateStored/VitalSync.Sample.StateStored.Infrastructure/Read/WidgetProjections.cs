using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Read;

public sealed class WidgetCreatedProjection(WidgetReadDbContext context) : IProjectionHandler<WidgetCreated>
{
    public async Task Handle(WidgetCreated domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

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
            });
        }
        else if (existing.RenameCount == 0)
        {
            existing.Name = domainEvent.Name;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class WidgetRenamedProjection(WidgetReadDbContext context) : IProjectionHandler<WidgetRenamed>
{
    public async Task Handle(WidgetRenamed domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

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
            });
        }
        else if (existing.RenameCount < domainEvent.RenameCount)
        {
            existing.Name = domainEvent.Name;
            existing.RenameCount = domainEvent.RenameCount;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
