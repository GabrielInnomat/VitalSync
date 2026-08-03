using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Infrastructure.Read;

public sealed class GadgetCreatedProjection(GadgetReadDbContext context) : IProjectionHandler<GadgetCreated>
{
    public async Task Handle(GadgetCreated domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

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

public sealed class GadgetRenamedProjection(GadgetReadDbContext context) : IProjectionHandler<GadgetRenamed>
{
    public async Task Handle(GadgetRenamed domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

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

public sealed class GadgetRetiredProjection(GadgetReadDbContext context) : IProjectionHandler<GadgetRetired>
{
    public async Task Handle(GadgetRetired domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

        var existing = await context.Gadgets
            .FirstOrDefaultAsync(gadget => gadget.Id == domainEvent.GadgetId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            context.Gadgets.Add(new GadgetReadModel
            {
                Id = domainEvent.GadgetId,
                IsRetired = true,
                Version = metadata.Version,
            });
        }
        else if (existing.Version < metadata.Version)
        {
            existing.IsRetired = true;
            existing.Version = metadata.Version;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
