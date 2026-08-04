namespace VitalSync.Sample.StateStored.Application;

public sealed record WidgetView(Guid Id, string Name, int RenameCount, int PartCount, int TotalQuantity);
