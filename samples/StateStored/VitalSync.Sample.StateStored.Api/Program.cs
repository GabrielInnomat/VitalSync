using BuildingBlocks.Infrastructure.DependencyInjection;
using ProtoBuf.Grpc.Server;
using VitalSync.Sample.StateStored.Api;
using VitalSync.Sample.StateStored.Infrastructure;
using VitalSync.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSampleStateStoredInfrastructure(
    builder.Configuration.GetConnectionString("statestored-write")!,
    builder.Configuration.GetConnectionString("statestored-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!));

builder.Services.AddCodeFirstGrpc();

builder.Host.UseWolverine(options =>
    options.UseBuildingBlocksEfCorePersistence(
        builder.Configuration.GetConnectionString("statestored-write")!));

var app = builder.Build();

app.MapGrpcService<WidgetGrpcService>();
app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
