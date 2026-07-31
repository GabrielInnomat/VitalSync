using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Application;

// The contract lives here because the query handler consumes it (ADR-0024); the implementation
// against the read database belongs to Infrastructure.
public interface IWidgetReadStore
{
    Task<WidgetView?> GetAsync(WidgetId id, CancellationToken cancellationToken);
}
