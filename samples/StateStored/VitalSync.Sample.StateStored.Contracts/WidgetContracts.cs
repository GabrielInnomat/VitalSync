using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace VitalSync.Sample.StateStored.Contracts;

// Code-first gRPC (ADR-0003): the contract is authored in C#, not in a .proto file.
//
// Its own library rather than a folder in the API, because both the service and its callers - the BFF
// eventually, the smoke test today - have to reference the same types. CA1515 makes that concrete: public
// types in an application project are flagged, and a gRPC contract is public by definition. The same
// question is still open for the integration event (ADR-0024), which stays in Infrastructure until stage 3
// gives it a second consumer.
//
// The numbers in [ProtoMember] are field identities on the wire, not a sort order: renumbering an existing
// field silently reinterprets old payloads. protobuf-net.BuildTools checks them at build time.
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

// Empty on purpose: the operation has no result beyond success, and a message type leaves room to add one
// without changing the signature.
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
