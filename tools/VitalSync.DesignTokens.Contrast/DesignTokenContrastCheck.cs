namespace VitalSync.DesignTokens.Contrast;

internal sealed record DesignTokenContrastCheck(
    IReadOnlyList<ContrastCheckResult> Results,
    IReadOnlyList<SeparationCheckResult> Separations,
    IReadOnlyList<string> Problems)
{
    public bool HasFatalFindings(bool strict) =>
        Results.Any(result => result.IsFatal(strict)) || Separations.Any(result => result.IsFatal(strict));

    public static DesignTokenContrastCheck TryCreate(string tokensPath, string rulesPath)
    {
        ArgumentNullException.ThrowIfNull(tokensPath);
        ArgumentNullException.ThrowIfNull(rulesPath);

        var problems = new List<string>();
        var tokensText = TryReadFile(tokensPath, problems);
        var rulesText = TryReadFile(rulesPath, problems);

        if ((tokensText is null) || (rulesText is null))
        {
            return new DesignTokenContrastCheck([], [], problems);
        }

        var ruleSet = ContrastRuleSet.Load(rulesText);

        if (ruleSet.RuleSet is null)
        {
            problems.AddRange(ruleSet.Problems);

            return new DesignTokenContrastCheck([], [], problems);
        }

        var tokens = CssCustomProperties.Parse(tokensText);

        return new DesignTokenContrastCheck(
            ContrastCheckRunner.Run(tokens, ruleSet.RuleSet),
            SeparationCheckRunner.Run(tokens, ruleSet.RuleSet),
            problems);
    }

    public static DesignTokenContrastCheck ForRepository()
    {
        var root = RepositoryLocator.RequireRoot();

        return TryCreate(RepositoryLocator.TokensPath(root), RepositoryLocator.RulesPath(root));
    }

    private static string? TryReadFile(string path, List<string> problems)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            problems.Add(FormattableString.Invariant($"{path} does not exist"));
        }
        catch (DirectoryNotFoundException)
        {
            problems.Add(FormattableString.Invariant($"{path} does not exist"));
        }
        catch (IOException exception)
        {
            problems.Add(FormattableString.Invariant($"{path} could not be read: {exception.Message}"));
        }
        catch (UnauthorizedAccessException exception)
        {
            problems.Add(FormattableString.Invariant($"{path} could not be read: {exception.Message}"));
        }

        return null;
    }
}
