using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VitalSync.Sample.StateStored.Infrastructure.Read;
using VitalSync.Sample.StateStored.Infrastructure.Write;

namespace VitalSync.Sample.StateStored.Infrastructure.DesignTime;

// `dotnet ef` needs to build the model, not reach a database - the connection string below is never
// connected to. Without these factories the tool would have to boot a host, which would drag in Wolverine,
// RabbitMQ and Aspire-provided connection strings just to scaffold a migration.
//
//   dotnet ef migrations add <Name> --context WidgetWriteDbContext -o Migrations/Write \
//     --project samples/StateStored/VitalSync.Sample.StateStored.Infrastructure
//
// Both contexts live in one assembly, so --context is mandatory.
internal static class DesignTimeConnectionString
{
    public const string Value = "Host=localhost;Database=design-time;Username=postgres;Password=postgres";
}

public sealed class WidgetWriteDbContextFactory : IDesignTimeDbContextFactory<WidgetWriteDbContext>
{
    public WidgetWriteDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<WidgetWriteDbContext>()
            .UseNpgsql(DesignTimeConnectionString.Value)
            .Options);
}

public sealed class WidgetReadDbContextFactory : IDesignTimeDbContextFactory<WidgetReadDbContext>
{
    public WidgetReadDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<WidgetReadDbContext>()
            .UseNpgsql(DesignTimeConnectionString.Value)
            .Options);
}
