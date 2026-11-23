namespace VitalSync.DesignSystem;

internal static class CssClasses
{
    public static string Merge(string componentClasses, IReadOnlyDictionary<string, object>? additionalAttributes) =>
        additionalAttributes is not null
        && additionalAttributes.TryGetValue("class", out var additional)
        && additional is string additionalClasses
        && !string.IsNullOrWhiteSpace(additionalClasses)
            ? $"{componentClasses} {additionalClasses.Trim()}"
            : componentClasses;
}
