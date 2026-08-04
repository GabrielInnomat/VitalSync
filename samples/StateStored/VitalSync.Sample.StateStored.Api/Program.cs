using ProtoBuf.Grpc.Server;
using VitalSync.Sample.StateStored.Api;
using VitalSync.Sample.StateStored.Infrastructure;
using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.AddSampleStateStoredInfrastructure(
    builder.Configuration.GetConnectionString("statestored-write")!,
    builder.Configuration.GetConnectionString("statestored-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!),
    VitalSyncMessaging.IntegrationEventExchangeName);

builder.Services.AddCodeFirstGrpc();

var app = builder.Build();

app.MapGrpcService<WidgetGrpcService>();
app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
