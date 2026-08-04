using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Application;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Read;

public sealed class WidgetReadStore(WidgetReadDbContext context) : IWidgetReadStore
{
    public Task<WidgetView?> GetAsync(WidgetId id, CancellationToken cancellationToken) =>
        context.Widgets
            .AsNoTracking()
            .Where(widget => widget.Id == id)
            .Select(widget => new WidgetView(
                widget.Id.Value,
                widget.Name,
                widget.RenameCount,
                widget.PartCount,
                widget.TotalQuantity))
            .FirstOrDefaultAsync(cancellationToken);
}
