using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Application;

public sealed record GetWidget(WidgetId WidgetId) : IQuery<WidgetView>;

public sealed class GetWidgetHandler(IWidgetReadStore readStore) : IQueryHandler<GetWidget, WidgetView>
{
    public async Task<Result<WidgetView>> HandleAsync(GetWidget query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var view = await readStore.GetAsync(query.WidgetId, cancellationToken).ConfigureAwait(false);

        return view is null
            ? Failure.NotFound("widget.not_found", $"No widget with id '{query.WidgetId.Value}' exists.")
            : view;
    }
}
