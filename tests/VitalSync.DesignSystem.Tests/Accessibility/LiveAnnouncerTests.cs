using Bunit;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.DesignSystem.Accessibility;

namespace VitalSync.DesignSystem.Tests.Accessibility;

public sealed class LiveAnnouncerTests : BunitContext
{
    public LiveAnnouncerTests()
    {
        Services.AddScoped<LiveAnnouncer>();
    }

    private LiveAnnouncer Announcer => Services.GetRequiredService<LiveAnnouncer>();

    [Fact]
    public void Renders_a_polite_and_an_urgent_region_that_start_empty()
    {
        var cut = Render<LiveAnnouncerRegion>();

        var regions = cut.FindAll(".vs-live-announcer");
        Assert.Equal(["status", "alert"], regions.Select(region => region.GetAttribute("role")));
        Assert.All(regions, region => Assert.Equal("false", region.GetAttribute("aria-atomic")));
        Assert.All(regions, region => Assert.Empty(region.Children));
    }

    [Fact]
    public async Task Appends_each_message_to_the_region_of_its_urgency()
    {
        var cut = Render<LiveAnnouncerRegion>();

        await cut.InvokeAsync(() =>
        {
            Announcer.Announce("  Rezepte geladen.  ");
            Announcer.AnnounceUrgent("Speichern fehlgeschlagen.");
            Announcer.Announce("Apfel hinzugefügt.");
        });

        Assert.Equal(
            ["Rezepte geladen.", "Apfel hinzugefügt."],
            cut.FindAll("[role=status] > div").Select(message => message.TextContent));
        Assert.Equal(
            ["Speichern fehlgeschlagen."],
            cut.FindAll("[role=alert] > div").Select(message => message.TextContent));
    }

    [Fact]
    public async Task Clears_a_message_once_its_delay_has_passed()
    {
        Announcer.ClearDelay = TimeSpan.FromMilliseconds(50);
        var cut = Render<LiveAnnouncerRegion>();

        await cut.InvokeAsync(() => Announcer.Announce("Rezepte geladen."));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=status] > div")));
        Assert.Empty(Announcer.Announcements);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_blank_message(string text)
    {
        Assert.Throws<ArgumentException>(() => Announcer.Announce(text));
        Assert.Throws<ArgumentException>(() => Announcer.AnnounceUrgent(text));
        Assert.Empty(Announcer.Announcements);
    }
}
