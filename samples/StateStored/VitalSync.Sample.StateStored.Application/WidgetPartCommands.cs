using BuildingBlocks.Application;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Application;

public sealed record AddWidgetPart(WidgetId WidgetId, string Label, int Quantity) : ICommand<WidgetPartId>;

public sealed class AddWidgetPartHandler(IRepository<Widget, WidgetId> repository)
    : ICommandHandler<AddWidgetPart, WidgetPartId>
{
    public async Task<Result<WidgetPartId>> Handle(AddWidgetPart command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var widget = await repository.GetByIdAsync(command.WidgetId, cancellationToken).ConfigureAwait(false);

        return widget is null
            ? Failure.NotFound("widget.not_found", $"No widget with id '{command.WidgetId.Value}' exists.")
            : widget.AddPart(command.Label, command.Quantity);
    }
}

public sealed record ChangeWidgetPartQuantity(WidgetId WidgetId, WidgetPartId PartId, int Quantity) : ICommand;

public sealed class ChangeWidgetPartQuantityHandler(IRepository<Widget, WidgetId> repository)
    : ICommandHandler<ChangeWidgetPartQuantity>
{
    public async Task<Result> Handle(ChangeWidgetPartQuantity command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var widget = await repository.GetByIdAsync(command.WidgetId, cancellationToken).ConfigureAwait(false);
        if (widget is null)
        {
            return Failure.NotFound("widget.not_found", $"No widget with id '{command.WidgetId.Value}' exists.");
        }

        widget.ChangePartQuantity(command.PartId, command.Quantity);
        return Result.Success();
    }
}

public sealed record RemoveWidgetPart(WidgetId WidgetId, WidgetPartId PartId) : ICommand<string>;

public sealed class RemoveWidgetPartHandler(IRepository<Widget, WidgetId> repository)
    : ICommandHandler<RemoveWidgetPart, string>
{
    public async Task<Result<string>> Handle(RemoveWidgetPart command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var widget = await repository.GetByIdAsync(command.WidgetId, cancellationToken).ConfigureAwait(false);
        if (widget is null)
        {
            return Failure.NotFound("widget.not_found", $"No widget with id '{command.WidgetId.Value}' exists.");
        }

        var label = widget.Parts.FirstOrDefault(part => part.Id == command.PartId)?.Label ?? string.Empty;
        widget.RemovePart(command.PartId);
        return label;
    }
}
