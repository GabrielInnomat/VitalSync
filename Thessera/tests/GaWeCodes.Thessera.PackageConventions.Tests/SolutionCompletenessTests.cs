using System.Xml.Linq;

namespace GaWeCodes.Thessera.Tests;

/// <summary>
/// Keeps Thessera.slnx in step with the projects that actually exist.
/// </summary>
/// <remarks>
/// A project missing from the solution still builds, because whatever references it drags it in.
/// That is exactly why the gap survives: nothing goes red. It shows up later as a package that no
/// one opened in the IDE, that no solution-wide analysis covered, and that a solution-scoped
/// `dotnet build` or `dotnet pack` quietly skipped.
/// </remarks>
public sealed class SolutionCompletenessTests
{
    [Fact]
    public void EveryProjectUnderSrcAndTests_IsListedInTheSolution()
    {
        var root = ThesseraRoot();

        var onDisk = Directory
            .EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => Relative(root, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var listed = ListedProjects(root).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal<IEnumerable<string>>(onDisk, listed);
    }

    [Fact]
    public void TheSolutionNamesNoProjectThatIsGone()
    {
        var root = ThesseraRoot();

        var missing = ListedProjects(root)
            .Where(project => !File.Exists(Path.Combine(root, project.Replace('/', Path.DirectorySeparatorChar))))
            .ToArray();

        Assert.Empty(missing);
    }

    private static string[] ListedProjects(string root) =>
    [
        .. XDocument.Load(Path.Combine(root, "Thessera.slnx"))
            .Descendants("Project")
            .Select(project => project.Attribute("Path")!.Value),
    ];

    private static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

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
