namespace VitalSync.DesignSystem.Patterns.Feedback;

public sealed record AlertBannerLink
{
    public AlertBannerLink(string text, string href)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(href);

        Text = text.Trim();
        Href = href.Trim();
    }

    public string Text { get; }

    public string Href { get; }
}
