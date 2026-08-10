using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VitalSync.Sample.EventSourced.Infrastructure.Read;

namespace VitalSync.Sample.EventSourced.MigrationService.DesignTime;

internal sealed class GadgetReadDbContextFactory : IDesignTimeDbContextFactory<GadgetReadDbContext>
{
    public GadgetReadDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<GadgetReadDbContext>()
            .UseNpgsql("Host=design-time")
            .Options);
}
