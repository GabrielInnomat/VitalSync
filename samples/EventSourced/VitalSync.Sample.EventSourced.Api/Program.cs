using ProtoBuf.Grpc.Server;
using VitalSync.Sample.EventSourced.Api;
using VitalSync.Sample.EventSourced.Infrastructure;
using VitalSync.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSampleEventSourcedInfrastructure(
    builder.Configuration.GetConnectionString("eventsourced-write")!,
    builder.Configuration.GetConnectionString("eventsourced-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!));

builder.Services.AddCodeFirstGrpc();

// Still bare, now on both halves of the messaging story: this service publishes its own integration events and
// consumes the state-stored context's, and neither needed a line here (ADR-0027). Stage 3 first wired the
// subscription in this file and then moved it into Building Blocks - see WalkingSkeleton.md for what that cost
// while it lived here.
builder.Host.UseWolverine();

var app = builder.Build();

app.MapGrpcService<GadgetGrpcService>();
app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
