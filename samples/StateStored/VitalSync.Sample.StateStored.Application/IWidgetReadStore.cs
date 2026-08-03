using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Application;

public interface IWidgetReadStore
{
    Task<WidgetView?> GetAsync(WidgetId id, CancellationToken cancellationToken);
}
