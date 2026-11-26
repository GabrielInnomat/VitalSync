namespace VitalSync.DesignSystem.Patterns.Data;

public sealed record VitalsTrend
{
    public VitalsTrend(VitalsTrendDirection direction, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Direction = direction;
        Text = text.Trim();
    }

    public VitalsTrendDirection Direction { get; }

    public string Text { get; }
}
