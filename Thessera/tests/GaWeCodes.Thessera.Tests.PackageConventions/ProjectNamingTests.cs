namespace GaWeCodes.Thessera.Tests;

/// <summary>
/// Pins the shape of every project name in the family.
/// </summary>
/// <remarks>
/// Three renames have passed over this repository and not one of them could have gone red: a
/// project name is a file name, so a wrong one still compiles, still runs and still ships. The rule
/// is short -- a source project is <c>GaWeCodes.Thessera.&lt;Package&gt;</c>; a test project either
/// mirrors exactly one package as <c>GaWeCodes.Thessera.&lt;Package&gt;.Tests</c> or says that it
/// mirrors none as <c>GaWeCodes.Thessera.Tests.&lt;Suite&gt;</c>; and the fixtures and matrix hosts
/// carry no prefix at all, because they stand in for a stranger's code. That last form is the one
/// worth guarding hardest: prefixing a matrix host would quietly turn the proof that a package can
/// be consumed from outside into a proof that it can be consumed from inside.
/// </remarks>
public sealed class ProjectNamingTests
{
    private const string Prefix = "GaWeCodes.Thessera.";
    private const string SuiteProjectPrefix = "GaWeCodes.Thessera.Tests.";
    private const string MirrorSuffix = ".Tests";

    [Fact]
    public void EverySourceProject_IsTheFamilyPrefixPlusAPackageName()
    {
        var projects = ProjectNames(Path.Combine(ThesseraRoot(), "src"));

        Assert.NotEmpty(projects);

        var offenders = projects
            .Where(name => !name.StartsWith(Prefix, StringComparison.Ordinal) || name.Length == Prefix.Length)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            Describe($"Every project under 'src' must be named '{Prefix}<Package>'.", offenders));
    }

    [Fact]
    public void EveryTestProject_EitherMirrorsOnePackageOrSaysThatItMirrorsNone()
    {
        var packages = ProjectNames(Path.Combine(ThesseraRoot(), "src"));
        var projects = TestProjectNames();

        Assert.NotEmpty(packages);
        Assert.NotEmpty(projects);

        var offenders = projects.Where(name => !IsPackageMirror(name, packages) && !IsSuite(name)).ToArray();

        Assert.True(
            offenders.Length == 0,
            Describe(
                $"A test project is either '{Prefix}<Package>{MirrorSuffix}' naming an existing package "
                + $"under 'src', or '{SuiteProjectPrefix}<Suite>' when it mirrors no single package.",
                offenders));
    }

    [Fact]
    public void TheFixturesAndMatrixHosts_CarryNoFamilyPrefix()
    {
        var projects = ExemptProjectNames();

        Assert.NotEmpty(projects);

        var offenders = projects.Where(name => name.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();

        Assert.True(
            offenders.Length == 0,
            Describe(
                "Fixtures and matrix hosts stand in for a stranger's code and must not carry the "
                + "family prefix; prefixing one turns an outside-in proof into an inside-in one.",
                offenders));
    }

    [Fact]
    public void EveryProjectFile_IsNamedAfterItsDirectory()
    {
        var root = ThesseraRoot();

        var projects = ProjectFiles(root);

        Assert.NotEmpty(projects);

        var offenders = projects
            .Where(path => Path.GetFileNameWithoutExtension(path)
                != Path.GetFileName(Path.GetDirectoryName(path)!))
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            Describe(
                "A project file carries its directory's name. A directory renamed without its "
                + "'.csproj' builds exactly as before and is invisible until someone reads the path.",
                offenders));
    }

    private static bool IsPackageMirror(string name, string[] packages) =>
        name.EndsWith(MirrorSuffix, StringComparison.Ordinal)
        && packages.Contains(name[..^MirrorSuffix.Length], StringComparer.Ordinal);

    private static bool IsSuite(string name) =>
        name.StartsWith(SuiteProjectPrefix, StringComparison.Ordinal) && name.Length > SuiteProjectPrefix.Length;

    private static string[] TestProjectNames()
    {
        var tests = Path.Combine(ThesseraRoot(), "tests");

        return
        [
            .. ProjectFiles(tests)
                .Where(path => !IsExempt(tests, path))
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => name!)
                .Order(StringComparer.Ordinal),
        ];
    }

    private static string[] ExemptProjectNames()
    {
        var tests = Path.Combine(ThesseraRoot(), "tests");

        return
        [
            .. ProjectFiles(tests)
                .Where(path => IsExempt(tests, path))
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => name!)
                .Order(StringComparer.Ordinal),
        ];
    }

    private static bool IsExempt(string tests, string path) =>
        Path.GetRelativePath(tests, path).Replace(Path.DirectorySeparatorChar, '/')
            .StartsWith("ExternalAssemblies/", StringComparison.Ordinal)
        || Path.GetRelativePath(tests, path).Replace(Path.DirectorySeparatorChar, '/')
            .StartsWith("MatrixHosts/", StringComparison.Ordinal);

    private static string[] ProjectNames(string directory) =>
    [
        .. ProjectFiles(directory).Select(Path.GetFileNameWithoutExtension).Select(name => name!).Order(StringComparer.Ordinal),
    ];

    private static string[] ProjectFiles(string directory) =>
    [
        .. Directory
            .EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)),
    ];

    private static string Describe(string rule, string[] offenders) =>
        $"{rule}{Environment.NewLine}Offending: {string.Join(", ", offenders)}";

    private static string ThesseraRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.EnumerateFiles(directory.FullName, "*.slnx").Any())
        {
            directory = directory.Parent;
        }

        Assert.True(
            directory is not null,
            "No directory containing a '*.slnx' file was found above "
            + $"'{AppContext.BaseDirectory}'; the Thessera root cannot be located.");

        return directory!.FullName;
    }
}
