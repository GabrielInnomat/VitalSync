using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace VitalSync.ServiceDefaults.Tests;

public class HealthEndpointSeparationTests
{
    private const string DeadLetterCheckName = "thessera-dead-letters";

    [Fact]
    public async Task ReadinessExcludesTheDeadLetterCheck_WhichKeepsItsOwnEndpoint()
    {
        await using var app = BuildApp();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var ready = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);
        var deadLetters = await client.GetAsync(new Uri("/health/dead-letters", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", await ready.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Unhealthy", await deadLetters.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LivenessSeesNeitherReadinessNorDeadLetters()
    {
        await using var app = BuildApp();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var alive = await client.GetAsync(new Uri("/alive", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", await alive.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [AspireExtensions.LiveTag])
            .AddCheck("write-db", () => HealthCheckResult.Healthy(), [AspireExtensions.ReadyTag])
            .AddCheck(
                DeadLetterCheckName,
                () => HealthCheckResult.Unhealthy("one dead letter"),
                [AspireExtensions.DeadLetterTag]);

        var app = builder.Build();
        app.MapDefaultEndpoints();
        return app;
    }
}
