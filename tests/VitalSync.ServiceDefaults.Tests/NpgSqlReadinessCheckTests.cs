using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitalSync.ServiceDefaults;

namespace VitalSync.ServiceDefaults.Tests;

public class NpgSqlReadinessCheckTests
{
    [Fact]
    public void AddNpgSqlReadinessCheck_WithUnknownConnectionName_Throws()
    {
        var builder = Host.CreateApplicationBuilder();

        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.AddNpgSqlReadinessCheck(connectionName: "nutrition-write", name: "nutrition-write"));

        Assert.Contains("nutrition-write", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddNpgSqlReadinessCheck_WithConfiguredConnectionName_RegistersReadyCheck()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:nutrition-write"] = "Host=localhost;Database=nutrition-write";

        builder.AddNpgSqlReadinessCheck(connectionName: "nutrition-write", name: "nutrition-write");

        using var host = builder.Build();
        var options = host.Services.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value;

        var registration = Assert.Single(options.Registrations, r => r.Name == "nutrition-write");
        Assert.Contains(AspireExtensions.ReadyTag, registration.Tags, StringComparer.Ordinal);
    }
}
