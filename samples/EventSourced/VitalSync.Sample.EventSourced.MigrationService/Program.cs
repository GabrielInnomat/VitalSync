using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Infrastructure.Read;
using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddDbContext<GadgetReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("eventsourced-read")));

var host = builder.Build();

using var scope = host.Services.CreateScope();

await scope.ServiceProvider.GetRequiredService<GadgetReadDbContext>()
    .Database.MigrateAsync().ConfigureAwait(false);
