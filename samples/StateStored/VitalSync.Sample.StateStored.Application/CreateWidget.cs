using BuildingBlocks.Application;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Application;

public sealed record CreateWidget(string Name) : ICommand<WidgetId>;

public sealed class CreateWidgetHandler(IRepository<Widget, WidgetId> repository)
    : ICommandHandler<CreateWidget, WidgetId>
{
    public async Task<Result<WidgetId>> Handle(CreateWidget command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var widget = Widget.Create(WidgetId.New(), command.Name);
        await repository.AddAsync(widget, cancellationToken).ConfigureAwait(false);

        return widget.Id;
    }
}
