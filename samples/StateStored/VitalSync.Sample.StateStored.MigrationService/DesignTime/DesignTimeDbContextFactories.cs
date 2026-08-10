using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VitalSync.Sample.StateStored.Infrastructure.Read;
using VitalSync.Sample.StateStored.Infrastructure.Write;

namespace VitalSync.Sample.StateStored.MigrationService.DesignTime;

internal static class DesignTimeConnectionString
{
    public const string Value = "Host=design-time";
}

internal sealed class WidgetWriteDbContextFactory : IDesignTimeDbContextFactory<WidgetWriteDbContext>
{
    public WidgetWriteDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<WidgetWriteDbContext>()
            .UseNpgsql(DesignTimeConnectionString.Value)
            .Options);
}

internal sealed class WidgetReadDbContextFactory : IDesignTimeDbContextFactory<WidgetReadDbContext>
{
    public WidgetReadDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<WidgetReadDbContext>()
            .UseNpgsql(DesignTimeConnectionString.Value)
            .Options);
}
