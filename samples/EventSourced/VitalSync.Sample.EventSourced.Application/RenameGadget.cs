using BuildingBlocks.Application;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

public sealed record RenameGadget(GadgetId GadgetId, string Name) : ICommand;

public sealed class RenameGadgetHandler(IRepository<Gadget, GadgetId> repository)
    : ICommandHandler<RenameGadget>
{
    public async Task<Result> Handle(RenameGadget command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Reads the whole stream and folds it - there is no current-state row to read. The version the fold
        // produces is what the commit asserts against, which is how optimistic concurrency works here.
        var gadget = await repository.GetByIdAsync(command.GadgetId, cancellationToken).ConfigureAwait(false);
        if (gadget is null)
        {
            return Failure.NotFound("gadget.not_found", $"No gadget with id '{command.GadgetId.Value}' exists.");
        }

        gadget.Rename(command.Name);
        return Result.Success();
    }
}
