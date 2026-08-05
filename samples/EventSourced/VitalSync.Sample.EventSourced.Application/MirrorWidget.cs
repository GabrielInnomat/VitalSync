using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

public sealed record MirrorWidget(Guid WidgetId, string Name) : ICommand;

public sealed class MirrorWidgetHandler(IRepository<Gadget, GadgetId> repository) : ICommandHandler<MirrorWidget>
{
    public async Task<Result> HandleAsync(MirrorWidget command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = new GadgetId(command.WidgetId);

        var existing = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result.Success();
        }

        var gadget = Gadget.Create(id, command.Name);
        await repository.AddAsync(gadget, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
