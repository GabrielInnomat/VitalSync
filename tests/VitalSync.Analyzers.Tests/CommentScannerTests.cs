namespace VitalSync.Analyzers.Tests;

public sealed class CommentScannerTests
{
    [Theory]
    [InlineData("deploy.yml", "steps:\n  - run: echo hi # note", 1)]
    [InlineData("deploy.yml", "# note\non: push", 1)]
    [InlineData("deploy.yml", "run: echo \"#1\" '# 2' ${#items[@]} $#", 0)]
    [InlineData("Backup.ps1", "Get-Item . # note", 1)]
    [InlineData("Backup.ps1", "<# note #>\nGet-Item .", 1)]
    [InlineData("Backup.ps1", "Write-Host '# a' \"# b `\" # c\" $a[#0]", 0)]
    [InlineData("Backup.ps1", "$sql = @'\n# not a comment\n'@\nGet-Item .", 0)]
    [InlineData("appsettings.json", "{ \"Url\": \"https://example.org\" /* note */ }", 1)]
    [InlineData("VitalSync.Web.csproj", "<Project><!-- note --></Project>", 1)]
    [InlineData(".editorconfig", "[*]\n# note\n; note\nindent_size = 4", 2)]
    [InlineData(".gitignore", "# note\nbin/", 1)]
    [InlineData("LICENSE", "# not a comment", 0)]
    public void Finds_comments_by_file_type(string path, string text, int count) =>
        Assert.Equal(count, CommentScanner.ForFile(path, text).Length);

    [Theory]
    [InlineData("Text <!-- note -->", 1)]
    [InlineData("Write `<!--` in prose, it is code there.", 0)]
    [InlineData("```powershell\nGet-Item . # note\n```", 1)]
    [InlineData("```bash\ncurl https://example.org/#anchor\n```", 0)]
    [InlineData("```text\n# heading-like line\n```", 0)]
    [InlineData("```csharp\nvar a = 1; // note\n```", 1)]
    public void Finds_comments_in_markdown_prose_and_code_blocks(string text, int count) =>
        Assert.Equal(count, CommentScanner.Markdown(text).Length);

    [Fact]
    public void Reports_the_position_of_a_comment_inside_a_code_block()
    {
        const string text = "Intro\n```powershell\nGet-Item . # note\n```";

        var comment = Assert.Single(CommentScanner.Markdown(text));

        Assert.Equal(text.IndexOf('#', StringComparison.Ordinal), comment.Start);
    }

    [Fact]
    public void Rejects_a_code_block_language_without_known_comment_syntax() =>
        Assert.Throws<NotSupportedException>(() => CommentScanner.Markdown("```cobol\nDISPLAY 'X'.\n```"));

    [Fact]
    public void Rejects_a_file_type_without_known_comment_syntax() =>
        Assert.Throws<NotSupportedException>(() => CommentScanner.ForFile("build.cake", "Task(\"x\");"));
}
