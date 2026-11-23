using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using VitalSync.DesignSystem.Accessibility;
using VitalSync.DesignSystem.Gallery;
using VitalSync.DesignSystem.Patterns.Feedback;

namespace VitalSync.DesignSystem.Tests.Gallery;

public sealed class GalleryLayoutTests : BunitContext
{
    [Fact]
    public void Reports_not_found_and_renders_nothing_outside_development()
    {
        Services.AddSingleton<IWebHostEnvironment>(new StubWebHostEnvironment("Production"));
        var notFound = false;
        Services.GetRequiredService<NavigationManager>().OnNotFound += (_, _) => notFound = true;

        var cut = Render<GalleryLayout>(parameters => parameters
            .Add(p => p.Body, "<h1>Galerie</h1>"));

        Assert.True(notFound);
        Assert.Empty(cut.FindAll("nav"));
        Assert.Empty(cut.FindAll("h1"));
    }

    [Fact]
    public void Renders_the_gallery_in_development()
    {
        Services.AddSingleton<IWebHostEnvironment>(new StubWebHostEnvironment("Development"));
        Services.AddScoped<LiveAnnouncer>();
        Services.AddScoped<ToastService>();
        Services.AddScoped<AlertBannerService>();
        var notFound = false;
        Services.GetRequiredService<NavigationManager>().OnNotFound += (_, _) => notFound = true;

        var cut = Render<GalleryLayout>(parameters => parameters
            .Add(p => p.Body, "<h1>Galerie</h1>"));

        Assert.False(notFound);
        Assert.NotEmpty(cut.FindAll("nav"));
        Assert.Equal("Galerie", cut.Find("main h1").TextContent);
        Assert.Single(cut.FindAll(".vs-live-announcer[role=status]"));
        Assert.Single(cut.FindAll(".vs-live-announcer[role=alert]"));
        Assert.Empty(cut.FindAll(".vs-toast-region [role=status]"));
    }

    private sealed class StubWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "VitalSync.DesignSystem.Tests";

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
