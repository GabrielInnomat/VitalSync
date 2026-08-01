using System.Runtime.Serialization;
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
// [Service] is protobuf-net.Grpc's own marker; [ServiceContract] would work too but drags in the WCF
// primitives package for nothing.
[Service]
public interface IWidgetService
{
    ValueTask<CreateWidgetReply> CreateAsync(CreateWidgetRequest request, CallContext context = default);

    ValueTask<RenameWidgetReply> RenameAsync(RenameWidgetRequest request, CallContext context = default);

    ValueTask<WidgetReply> GetAsync(GetWidgetRequest request, CallContext context = default);
}

[DataContract]
public sealed class CreateWidgetRequest
{
    [DataMember(Order = 1)]
    public string Name { get; set; } = string.Empty;
}

[DataContract]
public sealed class CreateWidgetReply
{
    [DataMember(Order = 1)]
    public string WidgetId { get; set; } = string.Empty;
}

[DataContract]
public sealed class RenameWidgetRequest
{
    [DataMember(Order = 1)]
    public string WidgetId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Name { get; set; } = string.Empty;
}

[DataContract]
public sealed class RenameWidgetReply
{
}

[DataContract]
public sealed class GetWidgetRequest
{
    [DataMember(Order = 1)]
    public string WidgetId { get; set; } = string.Empty;
}

[DataContract]
public sealed class WidgetReply
{
    [DataMember(Order = 1)]
    public string WidgetId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Name { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int RenameCount { get; set; }
}
