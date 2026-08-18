using GaWeCodes.Application.ReadModels;
using GaWeCodes.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Infrastructure.Read;

public sealed class GadgetReadModelRebuilder(GadgetReadDbContext context) : IReadModelRebuilder<Gadget, GadgetId>
{
    public Task ClearAsync(CancellationToken cancellationToken) =>
        context.Gadgets.ExecuteDeleteAsync(cancellationToken);

    public async Task RebuildAsync(Gadget aggregate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        context.Gadgets.Add(new GadgetReadModel
        {
            Id = aggregate.Id,
            Name = aggregate.Name,
            RenameCount = aggregate.RenameCount,
            IsRetired = aggregate.IsRetired,
            Version = ((IStateOwner)aggregate).Version,
        });

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
