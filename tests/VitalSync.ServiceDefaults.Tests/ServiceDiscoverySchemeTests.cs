using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using VitalSync.ServiceDefaults;

namespace VitalSync.ServiceDefaults.Tests;

public class ServiceDiscoverySchemeTests
{
    [Fact]
    public void AddServiceDefaults_ByDefault_AllowsOnlyHttps()
    {
        var schemes = AllowedSchemes(allowPlainHttp: null);

        Assert.Equal(["https"], schemes);
    }

    [Fact]
    public void AddServiceDefaults_WithPlainHttpAllowed_PrefersHttpsAndAcceptsHttp()
    {
        var schemes = AllowedSchemes(allowPlainHttp: "true");

        Assert.Equal(["https", "http"], schemes);
    }

    private static IList<string> AllowedSchemes(string? allowPlainHttp)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[AspireExtensions.AllowPlainHttpKey] = allowPlainHttp;
        builder.AddServiceDefaults();

        using var host = builder.Build();
        return host.Services.GetRequiredService<IOptions<ServiceDiscoveryOptions>>().Value.AllowedSchemes;
    }
}
