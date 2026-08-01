using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Infrastructure.Read;

// Delivery is at-least-once and ordering is not guaranteed (ADR-0022), and event sourcing changes neither:
// the projection handler receives a plain IDomainEvent, with the stream version left behind in the event
// store. So each handler has to be idempotent and order-aware on its own, exactly as in the state-stored
// sample - the difference is only which of the three fields each event is allowed to touch.
public sealed class GadgetCreatedProjection(GadgetReadDbContext context) : IProjectionHandler<GadgetCreated>
{
    public async Task Handle(GadgetCreated domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var existing = await context.Gadgets
            .FirstOrDefaultAsync(gadget => gadget.Id == domainEvent.GadgetId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            context.Gadgets.Add(new GadgetReadModel
            {
                Id = domainEvent.GadgetId,
                Name = domainEvent.Name,
                RenameCount = 0,
                IsRetired = false,
            });
        }
        else if (existing.RenameCount == 0)
        {
            // The row exists but no rename has been seen, so this event still owns the name: another handler
            // (a retirement arriving first) created the row without one. Writing it back unconditionally
            // would be wrong the other way round - a redelivered create would undo a rename - which is why
            // the same business ordinal that guards the rename handler guards this one.
            existing.Name = domainEvent.Name;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class GadgetRenamedProjection(GadgetReadDbContext context) : IProjectionHandler<GadgetRenamed>
{
    public async Task Handle(GadgetRenamed domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var existing = await context.Gadgets
            .FirstOrDefaultAsync(gadget => gadget.Id == domainEvent.GadgetId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // The create event has not been projected yet; dropping the rename would lose it, so the row is
            // built from what this event carries and the create projection will leave it alone.
            context.Gadgets.Add(new GadgetReadModel
            {
                Id = domainEvent.GadgetId,
                Name = domainEvent.Name,
                RenameCount = domainEvent.RenameCount,
            });
        }
        else if (existing.RenameCount < domainEvent.RenameCount)
        {
            // The business ordinal is the only ordering the handler has: an older rename never overwrites a
            // newer one, and the same rename applied twice is a no-op.
            existing.Name = domainEvent.Name;
            existing.RenameCount = domainEvent.RenameCount;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class GadgetRetiredProjection(GadgetReadDbContext context) : IProjectionHandler<GadgetRetired>
{
    public async Task Handle(GadgetRetired domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var existing = await context.Gadgets
            .FirstOrDefaultAsync(gadget => gadget.Id == domainEvent.GadgetId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // Retirement is terminal, so it needs no ordinal: the flag can only ever move from false to true,
            // whatever order the events arrive in.
            context.Gadgets.Add(new GadgetReadModel
            {
                Id = domainEvent.GadgetId,
                IsRetired = true,
            });
        }
        else
        {
            existing.IsRetired = true;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
