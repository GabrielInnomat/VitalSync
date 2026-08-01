using BuildingBlocks.Application;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

public sealed record CreateGadget(string Name) : ICommand<GadgetId>;

// Identical to the state-stored handler down to the line: IRepository<TAggregate, TKey> is one contract
// (ADR-0026), and which store is behind it is decided in the composition layer. AddAsync only tracks - the
// stream append happens when the unit of work commits.
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
