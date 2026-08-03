using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Infrastructure.Read;

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
            context.Gadgets.Add(new GadgetReadModel
            {
                Id = domainEvent.GadgetId,
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
