using System.Diagnostics;

namespace VitalSync.Analyzers.Tests;

public sealed class RepositoryCommentTests
{
    [Fact]
    public void No_file_in_the_repository_contains_a_comment()
    {
        var root = RepositoryRoot();
        var violations = new List<string>();

        foreach (var file in RepositoryFiles(root))
        {
            if (string.Equals(CommentScanner.FileType(file), ".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(Path.Combine(root, file));
            foreach (var comment in CommentScanner.ForFile(file, text))
            {
                violations.Add($"{file}:{LineOf(text, comment.Start)}");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Every_file_type_in_the_repository_has_a_known_comment_syntax()
    {
        var unknown = RepositoryFiles(RepositoryRoot()).Where(file => !CommentScanner.IsKnownFileType(file)).ToList();

        Assert.Empty(unknown);
    }

    private static int LineOf(string text, int position) =>
        text.AsSpan(0, position).Count('\n') + 1;

    private static List<string> RepositoryFiles(string root)
    {
        var startInfo = new ProcessStartInfo("git", "ls-files -z --cached --others --exclude-standard")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        using var git = Process.Start(startInfo) ?? throw new InvalidOperationException("git could not be started.");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Assert.Equal(0, git.ExitCode);

        return [.. output
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Where(file => File.Exists(Path.Combine(root, file)))];
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VitalSync.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("The repository root with VitalSync.slnx was not found.");
    }
}
