using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VitalSync.Sample.EventSourced.Infrastructure.Read;

namespace VitalSync.Sample.EventSourced.Infrastructure.DesignTime;

public sealed class GadgetReadDbContextFactory : IDesignTimeDbContextFactory<GadgetReadDbContext>
{
    public GadgetReadDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<GadgetReadDbContext>()
            .UseNpgsql("Host=localhost;Database=design-time;Username=postgres;Password=postgres")
            .Options);
}
