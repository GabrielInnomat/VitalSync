using GaWeCodes.Application.Cqrs;
using GaWeCodes.Application.Persistence;
using GaWeCodes.Application.Results;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

public sealed record RenameGadget(GadgetId GadgetId, string Name) : ICommand;

public sealed class RenameGadgetHandler(IRepository<Gadget, GadgetId> repository)
    : ICommandHandler<RenameGadget>
{
    public async Task<Result> HandleAsync(RenameGadget command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var gadget = await repository.GetByIdAsync(command.GadgetId, cancellationToken).ConfigureAwait(false);
        if (gadget is null)
        {
            return Failure.NotFound("gadget.not_found", $"No gadget with id '{command.GadgetId.Value}' exists.");
        }

        gadget.Rename(command.Name);
        return Result.Success();
    }
}
