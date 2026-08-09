using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Infrastructure;
using VitalSync.Sample.EventSourced.Infrastructure.Read;
using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddSampleEventSourcedInfrastructure(
    builder.Configuration.GetConnectionString("eventsourced-write")!,
    builder.Configuration.GetConnectionString("eventsourced-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!),
    VitalSyncMessaging.IntegrationEventExchangeName,
    InfrastructureProvisioning.AtStartup);

var host = builder.Build();

await host.StartAsync().ConfigureAwait(false);

using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<GadgetReadDbContext>()
        .Database.MigrateAsync().ConfigureAwait(false);
}

await host.StopAsync().ConfigureAwait(false);
