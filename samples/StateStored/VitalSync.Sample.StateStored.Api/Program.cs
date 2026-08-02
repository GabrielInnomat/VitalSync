using ProtoBuf.Grpc.Server;
using VitalSync.Sample.StateStored.Api;
using VitalSync.Sample.StateStored.Infrastructure;
using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// The write database is named once. Building Blocks owns UseWolverine and reads the connection string back
// from this selection, so the EF outbox cannot end up in a different database than the aggregates (ADR-0027
// amendment; before it, the host had to repeat the string in its own UseWolverine call).
builder.AddSampleStateStoredInfrastructure(
    builder.Configuration.GetConnectionString("statestored-write")!,
    builder.Configuration.GetConnectionString("statestored-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!));

builder.Services.AddCodeFirstGrpc();

var app = builder.Build();

app.MapGrpcService<WidgetGrpcService>();
app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
