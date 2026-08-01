namespace VitalSync.Sample.EventSourced.Application;

// Served from the read database, never by replaying the stream: querying an event store per request is
// exactly what the read model exists to avoid (ADR-0021/0022).
public sealed record GadgetView(Guid Id, string Name, int RenameCount, bool IsRetired);
