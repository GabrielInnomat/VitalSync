namespace VitalSync.DesignTokens.Contrast.Cli;

internal static class RepositoryLocator
{
    public const string SolutionFileName = "VitalSync.slnx";

    private static readonly string[] TokensSegments =
        ["src", "Frontend", "VitalSync.DesignSystem", "wwwroot", "vitalsync-tokens.css"];

    private static readonly string[] RulesSegments =
        ["src", "Frontend", "VitalSync.DesignSystem", "vitalsync-contrast-rules.json"];

    public static DirectoryInfo? FindRoot(string startDirectory)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);

        for (var current = new DirectoryInfo(startDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFileName)))
            {
                return current;
            }
        }

        return null;
    }

    public static DirectoryInfo RequireRoot() =>
        FindRoot(Directory.GetCurrentDirectory())
        ?? FindRoot(AppContext.BaseDirectory)
        ?? throw new DirectoryNotFoundException(
            FormattableString.Invariant($"Could not locate {SolutionFileName} above the current directory."));

    public static string TokensPath(DirectoryInfo root)
    {
        ArgumentNullException.ThrowIfNull(root);

        return Path.Combine([root.FullName, .. TokensSegments]);
    }

    public static string RulesPath(DirectoryInfo root)
    {
        ArgumentNullException.ThrowIfNull(root);

        return Path.Combine([root.FullName, .. RulesSegments]);
    }
}
