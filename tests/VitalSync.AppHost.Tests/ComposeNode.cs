namespace VitalSync.AppHost.Tests;

internal static class ComposeNode
{
    public static string? Scalar(Dictionary<object, object> node, string key) =>
        node.TryGetValue(key, out var value) ? value as string : null;

    public static IReadOnlyList<string> Sequence(Dictionary<object, object> node, string key) =>
        node.TryGetValue(key, out var value) && value is List<object> items ? [.. items.Cast<string>()] : [];

    public static Dictionary<object, object> Section(Dictionary<object, object> node, string key) =>
        node.TryGetValue(key, out var value) && value is Dictionary<object, object> section ? section : [];

    public static IReadOnlyList<Dictionary<object, object>> Mappings(Dictionary<object, object> node, string key) =>
        node.TryGetValue(key, out var value) && value is List<object> items
            ? [.. items.Cast<Dictionary<object, object>>()]
            : [];
}
