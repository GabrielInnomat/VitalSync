using BuildingBlocks.Application;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Application;

public sealed record RenameWidget(WidgetId WidgetId, string Name) : ICommand;

public sealed class RenameWidgetHandler(IRepository<Widget, WidgetId> repository)
    : ICommandHandler<RenameWidget>
{
    public async Task<Result> Handle(RenameWidget command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var widget = await repository.GetByIdAsync(command.WidgetId, cancellationToken).ConfigureAwait(false);
        if (widget is null)
        {
            return Failure.NotFound("widget.not_found", $"No widget with id '{command.WidgetId.Value}' exists.");
        }

        widget.Rename(command.Name);
        return Result.Success();
    }
}
