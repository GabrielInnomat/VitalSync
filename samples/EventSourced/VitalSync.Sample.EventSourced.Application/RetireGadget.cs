using BuildingBlocks.Application;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

public sealed record RetireGadget(GadgetId GadgetId, string Reason) : ICommand;

public sealed class RetireGadgetHandler(IRepository<Gadget, GadgetId> repository)
    : ICommandHandler<RetireGadget>
{
    public async Task<Result> Handle(RetireGadget command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var gadget = await repository.GetByIdAsync(command.GadgetId, cancellationToken).ConfigureAwait(false);
        if (gadget is null)
        {
            return Failure.NotFound("gadget.not_found", $"No gadget with id '{command.GadgetId.Value}' exists.");
        }

        gadget.Retire(command.Reason);
        return Result.Success();
    }
}
