using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace VitalSync.Sample.StateStored.Contracts;

[Service]
public interface IWidgetService
{
    ValueTask<CreateWidgetReply> CreateAsync(CreateWidgetRequest request, CallContext context = default);

    ValueTask<RenameWidgetReply> RenameAsync(RenameWidgetRequest request, CallContext context = default);

    ValueTask<WidgetReply> GetAsync(GetWidgetRequest request, CallContext context = default);
}

[ProtoContract]
public sealed class CreateWidgetRequest
{
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class CreateWidgetReply
{
    [ProtoMember(1)]
    public string WidgetId { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class RenameWidgetRequest
{
    [ProtoMember(1)]
    public string WidgetId { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class RenameWidgetReply
{
}

[ProtoContract]
public sealed class GetWidgetRequest
{
    [ProtoMember(1)]
    public string WidgetId { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class WidgetReply
{
    [ProtoMember(1)]
    public string WidgetId { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    [ProtoMember(3)]
    public int RenameCount { get; set; }
}
