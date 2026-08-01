using BuildingBlocks.Application;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

// What arrives from the other bounded context, expressed in this context's language. The handler is a normal
// command handler: the fact that the trigger came over RabbitMQ is the transport's business and stops at the
// Infrastructure boundary (ADR-0023 - Wolverine is transport, not mediator).
public sealed record MirrorWidget(Guid WidgetId, string Name) : ICommand;

public sealed class MirrorWidgetHandler(IRepository<Gadget, GadgetId> repository) : ICommandHandler<MirrorWidget>
{
    public async Task<Result> Handle(MirrorWidget command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Reusing the widget's own identifier is what makes the whole cross-service path idempotent without
        // any extra bookkeeping: delivery is at-least-once, and a freshly generated id would create another
        // gadget on every redelivery. The two contexts do not share a database, only this value.
        var id = new GadgetId(command.WidgetId);

        var existing = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            // Already mirrored. Reporting success rather than a conflict is deliberate: the message has been
            // dealt with, and a failure here would send the broker into a retry loop over a fact that is true.
            return Result.Success();
        }

        var gadget = Gadget.Create(id, command.Name);
        await repository.AddAsync(gadget, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
