using BuildingBlocks.Application.ReadModels;
using BuildingBlocks.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure.Read;
using VitalSync.Sample.StateStored.Infrastructure.Write;
using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddDbContext<WidgetWriteDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("statestored-write")));

builder.Services.AddDbContext<WidgetReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("statestored-read")));

builder.Services.AddScoped<IReadModelRebuilder<Widget, WidgetId>, WidgetReadModelRebuilder>();
builder.Services.AddSingleton<ReadModelRebuildRunner<WidgetWriteDbContext>>();

var host = builder.Build();

using var scope = host.Services.CreateScope();

await scope.ServiceProvider.GetRequiredService<WidgetWriteDbContext>()
    .Database.MigrateAsync().ConfigureAwait(false);

await scope.ServiceProvider.GetRequiredService<WidgetReadDbContext>()
    .Database.MigrateAsync().ConfigureAwait(false);

if (builder.Configuration.GetValue<bool>("ReadModels:Rebuild"))
{
    await host.Services.GetRequiredService<ReadModelRebuildRunner<WidgetWriteDbContext>>()
        .RebuildAsync<Widget, WidgetId, WidgetState>(CancellationToken.None).ConfigureAwait(false);
}

