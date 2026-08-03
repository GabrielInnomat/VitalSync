using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

public interface IGadgetReadStore
{
    Task<GadgetView?> GetAsync(GadgetId id, CancellationToken cancellationToken);
}
