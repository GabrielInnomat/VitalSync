namespace VitalSync.DesignTokens.Contrast.Cli;

internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFindings = 1;
    private const int ExitUsage = 2;

    internal static int Main(string[] args)
    {
        if (args.Any(argument => string.Equals(argument, "--help", StringComparison.Ordinal)
            || string.Equals(argument, "-h", StringComparison.Ordinal)))
        {
            WriteUsage();

            return ExitSuccess;
        }

        string tokensPath;
        string rulesPath;
        bool strict;

        try
        {
            var root = RepositoryLocator.RequireRoot();

            tokensPath = ReadOption(args, "--tokens") ?? RepositoryLocator.TokensPath(root);

            rulesPath = ReadOption(args, "--rules") ?? RepositoryLocator.RulesPath(root);

            strict = args.Any(argument => string.Equals(argument, "--strict", StringComparison.Ordinal));
        }
        catch (DirectoryNotFoundException exception)
        {
            Console.Error.WriteLine(exception.Message);

            return ExitUsage;
        }

        var check = DesignTokenContrastCheck.TryCreate(tokensPath, rulesPath);

        if (check.Problems.Count > 0)
        {
            Console.Error.WriteLine("The contrast check could not run:");

            foreach (var problem in check.Problems)
            {
                Console.Error.Write("  - ");
                Console.Error.WriteLine(problem);
            }

            return ExitUsage;
        }

        Console.WriteLine("VitalSync design token contrast check");
        Console.WriteLine(FormattableString.Invariant($"  tokens : {tokensPath}"));
        Console.WriteLine(FormattableString.Invariant($"  rules  : {rulesPath}"));
        Console.WriteLine(FormattableString.Invariant($"  mode   : {(strict ? "strict, waivers ignored" : "waivers honoured")}"));
        Console.WriteLine();
        Console.Write(ContrastReport.Render(check.Results, check.Separations, strict));

        return check.HasFatalFindings(strict) ? ExitFindings : ExitSuccess;
    }

    private static string? ReadOption(string[] args, string name)
    {
        for (var index = 0; index < (args.Length - 1); index++)
        {
            if (string.Equals(args[index], name, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("Usage: dotnet run --project tools/VitalSync.DesignTokens.Contrast [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --tokens <path>  Token stylesheet to inspect (default: the design system RCL wwwroot)");
        Console.WriteLine("  --rules <path>   Rule document to apply (default: next to the token stylesheet in the RCL)");
        Console.WriteLine("  --strict         Report waived findings as failures as well");
        Console.WriteLine("  --help           Show this text");
        Console.WriteLine();
        Console.WriteLine("Exit codes: 0 clean, 1 findings, 2 the check could not run");
    }
}
