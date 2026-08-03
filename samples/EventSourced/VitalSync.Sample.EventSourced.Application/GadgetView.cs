namespace VitalSync.Sample.EventSourced.Application;

public sealed record GadgetView(Guid Id, string Name, int RenameCount, bool IsRetired);
