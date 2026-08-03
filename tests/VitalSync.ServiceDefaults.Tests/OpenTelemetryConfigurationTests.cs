using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace VitalSync.ServiceDefaults.Tests;

public class OpenTelemetryConfigurationTests
{
    [Theory]
    [InlineData("Npgsql")]
    [InlineData("Wolverine")]
    [InlineData("Marten")]
    public void ConfigureOpenTelemetry_ListensToInfrastructureActivitySources(string sourceName)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.ConfigureOpenTelemetry();

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<TracerProvider>();

        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("test-span");

        Assert.NotNull(activity);
    }

    [Fact]
    public void ConfigureOpenTelemetry_IgnoresUnregisteredActivitySources()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.ConfigureOpenTelemetry();

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<TracerProvider>();

        using var source = new ActivitySource("VitalSync.Tests.NotRegistered");
        using var activity = source.StartActivity("test-span");

        Assert.Null(activity);
    }
}
