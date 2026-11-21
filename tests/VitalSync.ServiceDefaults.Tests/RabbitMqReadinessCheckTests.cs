using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VitalSync.ServiceDefaults;

namespace VitalSync.ServiceDefaults.Tests;

public class RabbitMqReadinessCheckTests
{
    [Fact]
    public void AddRabbitMqReadinessCheck_WithUnknownConnectionName_Throws()
    {
        var builder = Host.CreateApplicationBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddRabbitMqReadinessCheck());

        Assert.Contains("messaging", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRabbitMqReadinessCheck_WithConfiguredConnectionName_RegistersReadyCheck()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672";

        builder.AddRabbitMqReadinessCheck();

        using var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var registration = Assert.Single(options.Registrations, r => r.Name == "rabbitmq");
        Assert.Contains(AspireExtensions.ReadyTag, registration.Tags, StringComparer.Ordinal);
    }

    [Fact]
    public async Task RabbitMqReadinessCheck_WithoutARegisteredConnection_ReachesForTheBroker()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:messaging"] = "amqp://guest:guest@127.0.0.1:1";
        builder.AddRabbitMqReadinessCheck();

        using var host = builder.Build();
        var report = await host.Services.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(TestContext.Current.CancellationToken);

        var entry = report.Entries["rabbitmq"];
        Assert.Equal(HealthStatus.Unhealthy, entry.Status);
        Assert.NotNull(entry.Exception);
        Assert.IsNotType<InvalidOperationException>(entry.Exception);
    }
}
