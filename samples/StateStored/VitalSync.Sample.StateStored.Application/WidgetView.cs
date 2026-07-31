namespace VitalSync.Sample.StateStored.Application;

// The query-side shape. It is served from the read database, never from the aggregate (ADR-0021/0022).
public sealed record WidgetView(Guid Id, string Name, int RenameCount);
