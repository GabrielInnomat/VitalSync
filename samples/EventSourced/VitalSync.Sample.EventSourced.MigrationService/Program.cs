using GaWeCodes.Application.ReadModels;
using GaWeCodes.Core.DependencyInjection;
using GaWeCodes.Persistence.Marten.ReadModels;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Domain;
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

builder.Services.AddScoped<IReadModelRebuilder<Gadget, GadgetId>, GadgetReadModelRebuilder>();

var host = builder.Build();

await host.StartAsync().ConfigureAwait(false);

using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<GadgetReadDbContext>()
        .Database.MigrateAsync().ConfigureAwait(false);
}

if (builder.Configuration.GetValue<bool>("ReadModels:Rebuild"))
{
    await host.Services.GetRequiredService<EventSourcedReadModelRebuildRunner>()
        .RebuildAsync<Gadget, GadgetId>(CancellationToken.None).ConfigureAwait(false);
}

await host.StopAsync().ConfigureAwait(false);
