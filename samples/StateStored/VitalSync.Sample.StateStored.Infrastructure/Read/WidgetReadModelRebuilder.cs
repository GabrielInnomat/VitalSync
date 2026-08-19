using GaWeCodes.Thessera.Application.ReadModels;
using GaWeCodes.Thessera.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Read;

public sealed class WidgetReadModelRebuilder(WidgetReadDbContext context) : IReadModelRebuilder<Widget, WidgetId>
{
    public Task ClearAsync(CancellationToken cancellationToken) =>
        context.Widgets.ExecuteDeleteAsync(cancellationToken);

    public async Task RebuildAsync(Widget aggregate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var parts = aggregate.Parts;

        context.Widgets.Add(new WidgetReadModel
        {
            Id = aggregate.Id,
            Name = aggregate.Name,
            RenameCount = aggregate.RenameCount,
            PartCount = parts.Count,
            TotalQuantity = parts.Sum(part => part.Quantity),
            Version = ((IStateOwner)aggregate).Version,
        });

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
