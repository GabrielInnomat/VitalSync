using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Infrastructure.Read;
using VitalSync.Sample.StateStored.Infrastructure.Write;
using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddDbContext<WidgetWriteDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("statestored-write")));

builder.Services.AddDbContext<WidgetReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("statestored-read")));

var host = builder.Build();

using var scope = host.Services.CreateScope();

await scope.ServiceProvider.GetRequiredService<WidgetWriteDbContext>()
    .Database.MigrateAsync().ConfigureAwait(false);

await scope.ServiceProvider.GetRequiredService<WidgetReadDbContext>()
    .Database.MigrateAsync().ConfigureAwait(false);
