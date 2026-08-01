using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Infrastructure.Read;

public sealed class GadgetReadStore(GadgetReadDbContext context) : IGadgetReadStore
{
    public Task<GadgetView?> GetAsync(GadgetId id, CancellationToken cancellationToken) =>
        context.Gadgets
            .AsNoTracking()
            .Where(gadget => gadget.Id == id)
            .Select(gadget => new GadgetView(gadget.Id.Value, gadget.Name, gadget.RenameCount, gadget.IsRetired))
            .FirstOrDefaultAsync(cancellationToken);
}
