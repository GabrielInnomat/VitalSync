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

// The bare call ADR-0027 promises - no Wolverine configuration in the host at all. The state-stored service
// cannot do this: its EF Core outbox has to be applied here (see WolverineHostExtensions). Marten contributes
// its message store from the service collection instead, so nothing is left over for the host.
builder.Host.UseWolverine();

var app = builder.Build();

app.MapGrpcService<GadgetGrpcService>();
app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
