using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace VitalSync.Sample.EventSourced.Contracts;

// Code-first gRPC (ADR-0003), in its own library for the same reason as the state-stored contract: callers
// need the types and CA1515 keeps public types out of an application project.
//
// The numbers in [ProtoMember] are field identities on the wire, not a sort order.
[Service]
public interface IGadgetService
{
    ValueTask<CreateGadgetReply> CreateAsync(CreateGadgetRequest request, CallContext context = default);

    ValueTask<RenameGadgetReply> RenameAsync(RenameGadgetRequest request, CallContext context = default);

    ValueTask<RetireGadgetReply> RetireAsync(RetireGadgetRequest request, CallContext context = default);

    ValueTask<GadgetReply> GetAsync(GetGadgetRequest request, CallContext context = default);
}

[ProtoContract]
public sealed class CreateGadgetRequest
{
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class CreateGadgetReply
{
    [ProtoMember(1)]
    public string GadgetId { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class RenameGadgetRequest
{
    [ProtoMember(1)]
    public string GadgetId { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;
}

// Empty on purpose: the operation has no result beyond success, and a message type leaves room to add one
// without changing the signature.
[ProtoContract]
public sealed class RenameGadgetReply
{
}

[ProtoContract]
public sealed class RetireGadgetRequest
{
    [ProtoMember(1)]
    public string GadgetId { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Reason { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class RetireGadgetReply
{
}

[ProtoContract]
public sealed class GetGadgetRequest
{
    [ProtoMember(1)]
    public string GadgetId { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class GadgetReply
{
    [ProtoMember(1)]
    public string GadgetId { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    [ProtoMember(3)]
    public int RenameCount { get; set; }

    [ProtoMember(4)]
    public bool IsRetired { get; set; }
}
