using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Application;

// Consumed by the query handler, so it lives here (ADR-0024); the read-database implementation is Infrastructure's.
public interface IGadgetReadStore
{
    Task<GadgetView?> GetAsync(GadgetId id, CancellationToken cancellationToken);
}
