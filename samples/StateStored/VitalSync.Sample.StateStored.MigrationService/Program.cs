using BuildingBlocks.Application.ReadModels;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure;
using VitalSync.Sample.StateStored.Infrastructure.Read;
using VitalSync.Sample.StateStored.Infrastructure.Write;
using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddSampleStateStoredInfrastructure(
    builder.Configuration.GetConnectionString("statestored-write")!,
    builder.Configuration.GetConnectionString("statestored-read")!,
    new Uri(builder.Configuration.GetConnectionString("messaging")!),
    VitalSyncMessaging.IntegrationEventExchangeName,
    InfrastructureProvisioning.AtStartup);

builder.Services.AddScoped<IReadModelRebuilder<Widget, WidgetId>, WidgetReadModelRebuilder>();
builder.Services.AddSingleton<StateStoredReadModelRebuildRunner<WidgetWriteDbContext>>();

var host = builder.Build();

await host.StartAsync().ConfigureAwait(false);

using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<WidgetWriteDbContext>()
        .Database.MigrateAsync().ConfigureAwait(false);

    await scope.ServiceProvider.GetRequiredService<WidgetReadDbContext>()
        .Database.MigrateAsync().ConfigureAwait(false);
}

if (builder.Configuration.GetValue<bool>("ReadModels:Rebuild"))
{
    await host.Services.GetRequiredService<StateStoredReadModelRebuildRunner<WidgetWriteDbContext>>()
        .RebuildAsync<Widget, WidgetId, WidgetState>(CancellationToken.None).ConfigureAwait(false);
}

await host.StopAsync().ConfigureAwait(false);
