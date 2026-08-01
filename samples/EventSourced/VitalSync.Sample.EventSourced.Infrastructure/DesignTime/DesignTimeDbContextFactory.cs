using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VitalSync.Sample.EventSourced.Infrastructure.Read;

namespace VitalSync.Sample.EventSourced.Infrastructure.DesignTime;

// `dotnet ef` needs to build the model, not reach a database - the connection string below is never
// connected to.
//
//   dotnet ef migrations add <Name> -o Migrations \
//     --project samples/EventSourced/VitalSync.Sample.EventSourced.Infrastructure
//
// Only one context exists here, so --context is unnecessary: the event store has no EF model to scaffold.
public sealed class GadgetReadDbContextFactory : IDesignTimeDbContextFactory<GadgetReadDbContext>
{
    public GadgetReadDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<GadgetReadDbContext>()
            .UseNpgsql("Host=localhost;Database=design-time;Username=postgres;Password=postgres")
            .Options);
}
