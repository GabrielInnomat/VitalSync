using BuildingBlocks.Application;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

public sealed record GetGadget(GadgetId GadgetId) : IQuery<GadgetView>;

public sealed class GetGadgetHandler(IGadgetReadStore readStore) : IQueryHandler<GetGadget, GadgetView>
{
    public async Task<Result<GadgetView>> HandleAsync(GetGadget query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var view = await readStore.GetAsync(query.GadgetId, cancellationToken).ConfigureAwait(false);

        return view is null
            ? Failure.NotFound("gadget.not_found", $"No gadget with id '{query.GadgetId.Value}' exists.")
            : view;
    }
}
