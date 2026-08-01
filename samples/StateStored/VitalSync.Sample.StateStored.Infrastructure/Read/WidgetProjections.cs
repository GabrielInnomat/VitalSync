using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Read;

// Delivery is at-least-once (ADR-0022), so both handlers must survive seeing the same event twice.
// They are written as upserts rather than inserts/increments for exactly that reason.
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
            // Only while no rename has been projected does this event still own the name. Writing it back
            // unconditionally would let a redelivered create - or one arriving after a rename - resurrect
            // the original name, so the create handler needs the same business ordinal as the rename one.
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
            // The create event has not been projected yet. Ordering across events is not guaranteed by
            // anything today (see WalkingSkeleton.md) - dropping the rename would lose it, so the row is
            // created from what this event carries and the create projection will fill in the rest.
            context.Widgets.Add(new WidgetReadModel
            {
                Id = domainEvent.WidgetId,
                Name = domainEvent.Name,
                RenameCount = domainEvent.RenameCount,
            });
        }
        else if (existing.RenameCount < domainEvent.RenameCount)
        {
            // Guards against both redelivery and out-of-order arrival: an older rename never overwrites
            // a newer one, and the same rename applied twice is a no-op.
            existing.Name = domainEvent.Name;
            existing.RenameCount = domainEvent.RenameCount;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
