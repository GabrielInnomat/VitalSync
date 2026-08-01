using ProtoBuf.Grpc.Server;
using VitalSync.Sample.EventSourced.Api;
using VitalSync.Sample.EventSourced.Infrastructure;
using VitalSync.ServiceDefaults;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSampleEventSourcedInfrastructure(
    builder.Configuration.GetConnectionString("eventsourced-write")!,
    builder.Configuration.GetConnectionString("eventsourced-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!));

builder.Services.AddCodeFirstGrpc();

// This context's own queue. The name is the consumer's choice; nothing on the publishing side knows it.
const string SubscriptionQueueName = "eventsourced.integration-events";

// Until stage 3 this was a bare UseWolverine() - the one host in the repository that proved ADR-0027 in full.
// Subscribing is what took that away: Building Blocks wires only the publish half, so a consumer has to
// declare its own queue, binding, inbox and handler discovery. Whether this stays here or moves into
// BuildingBlocksOptions is the open decision this stage exists to force; see WalkingSkeleton.md.
builder.Host.UseWolverine(options =>
{
    // Wolverine scans the entry assembly only. The consumer lives in Infrastructure, so without this line the
    // integration event arrives and is discarded as unhandled - no error, no log at warning level.
    options.Discovery.IncludeAssembly(typeof(SampleEventSourcedInfrastructure).Assembly);

    // The queue belongs to the consumer (ADR-0023): the publisher knows only the exchange. Durable, because a
    // restart between delivery and handling must not lose the message.
    options.ListenToRabbitQueue(SubscriptionQueueName).UseDurableInbox();

    // The exchange name is a literal here because Building Blocks keeps its own constant internal - the
    // consumer cannot reference the value it must match.
    options.UseRabbitMq()
        .BindExchange("vitalsync.integration-events")
        .ToQueue(SubscriptionQueueName, bindingKey: "sample.*");
});

var app = builder.Build();

app.MapGrpcService<GadgetGrpcService>();
app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
