using ProtoBuf.Grpc.Server;
using VitalSync.Sample.EventSourced.Api;
using VitalSync.Sample.EventSourced.Infrastructure;
using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Not even a bare UseWolverine() any more: this service publishes its own integration events and consumes the
// state-stored context's, and Building Blocks wires both halves plus the runtime itself (ADR-0027). Stage 3
// first wired the subscription in this file and then moved it into Building Blocks - see WalkingSkeleton.md
// for what that cost while it lived here.
builder.AddSampleEventSourcedInfrastructure(
    builder.Configuration.GetConnectionString("eventsourced-write")!,
    builder.Configuration.GetConnectionString("eventsourced-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!));

builder.Services.AddCodeFirstGrpc();

var app = builder.Build();

app.MapGrpcService<GadgetGrpcService>();
app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
