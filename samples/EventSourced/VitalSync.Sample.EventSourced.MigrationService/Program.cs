using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Infrastructure.Read;
using VitalSync.ServiceDefaults;

// Deliberately asymmetric to the state-stored migration worker: only the read database has an EF Core
// schema to migrate. The write database's schema belongs to Marten, and Wolverine's message store belongs to
// Wolverine - both build their own tables when the API starts. Whether that asymmetry is acceptable is one of
// the questions this stage exists to answer (see WalkingSkeleton.md).
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddDbContext<GadgetReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("eventsourced-read")));

var host = builder.Build();

// Migrate and exit: Aspire's WaitForCompletion gates the API on this process finishing, and a failure has to
// surface as a non-zero exit code, which is what letting the exception escape Main produces.
using var scope = host.Services.CreateScope();

await scope.ServiceProvider.GetRequiredService<GadgetReadDbContext>()
    .Database.MigrateAsync().ConfigureAwait(false);
