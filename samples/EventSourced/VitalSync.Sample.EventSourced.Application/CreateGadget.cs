using BuildingBlocks.Application;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

public sealed record CreateGadget(string Name) : ICommand<GadgetId>;

public sealed class CreateGadgetHandler(IRepository<Gadget, GadgetId> repository)
    : ICommandHandler<CreateGadget, GadgetId>
{
    public async Task<Result<GadgetId>> Handle(CreateGadget command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var gadget = Gadget.Create(GadgetId.New(), command.Name);
        await repository.AddAsync(gadget, cancellationToken).ConfigureAwait(false);

        return gadget.Id;
    }
}
