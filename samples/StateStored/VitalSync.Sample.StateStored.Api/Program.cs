using BuildingBlocks.Infrastructure.DependencyInjection;
using ProtoBuf.Grpc.Server;
using VitalSync.Sample.StateStored.Api;
using VitalSync.Sample.StateStored.Infrastructure;
using VitalSync.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ADR-0027: the host states what it needs and configures nothing. No DbContext registration, no Wolverine
// options, no outbox, no routing - all of that is derived from these two connection strings and the broker
// URI inside Building Blocks.
builder.Services.AddSampleStateStoredInfrastructure(
    builder.Configuration.GetConnectionString("statestored-write")!,
    builder.Configuration.GetConnectionString("statestored-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!));

builder.Services.AddCodeFirstGrpc();
builder.Services.AddCodeFirstGrpcReflection();

// Handler discovery, the domain-event queue, the RabbitMQ transport and its routing all come from the
// registered IWolverineExtension. The EF outbox is the one exception ADR-0027 cannot cover: Wolverine 3.0
// forbids a container-registered extension from modifying the service collection, and both the message
// store and the transactional middleware do exactly that.
builder.Host.UseWolverine(options =>
    options.UseBuildingBlocksEfCorePersistence(
        builder.Configuration.GetConnectionString("statestored-write")!));

var app = builder.Build();

app.MapGrpcService<WidgetGrpcService>();
app.MapCodeFirstGrpcReflectionService();
app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
