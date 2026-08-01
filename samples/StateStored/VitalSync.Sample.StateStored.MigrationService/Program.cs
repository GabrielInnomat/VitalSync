using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Infrastructure.Read;
using VitalSync.Sample.StateStored.Infrastructure.Write;
using VitalSync.ServiceDefaults;

// Both contexts are registered plainly here, deliberately not through AddBuildingBlocks: migrating needs
// neither Wolverine, nor an outbox, nor a dispatcher, and pulling them in would make a schema job depend
// on a broker being reachable.
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddDbContext<WidgetWriteDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("statestored-write")));

builder.Services.AddDbContext<WidgetReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("statestored-read")));

var host = builder.Build();

// Migrate and exit rather than running the host: Aspire's WaitForCompletion gates the API on this
// process finishing, and a failure has to surface as a non-zero exit code. Letting the exception escape
// Main is what produces that - swallowing it would let the API start against an unmigrated database.
// EF Core logs the migrations it applies, so no additional logging is needed here.
using var scope = host.Services.CreateScope();

await scope.ServiceProvider.GetRequiredService<WidgetWriteDbContext>()
    .Database.MigrateAsync().ConfigureAwait(false);

await scope.ServiceProvider.GetRequiredService<WidgetReadDbContext>()
    .Database.MigrateAsync().ConfigureAwait(false);
