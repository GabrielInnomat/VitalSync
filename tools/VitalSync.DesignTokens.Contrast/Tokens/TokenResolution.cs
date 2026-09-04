namespace VitalSync.DesignTokens.Contrast.Tokens;

internal sealed record TokenResolution(string Reference, string? RawValue, SrgbColor? Color, string? Failure)
{
    public static TokenResolution Resolved(string reference, string rawValue, SrgbColor color) =>
        new(reference, rawValue, color, Failure: null);

    public static TokenResolution Unresolved(string reference, string failure) =>
        new(reference, RawValue: null, Color: null, failure);
}
