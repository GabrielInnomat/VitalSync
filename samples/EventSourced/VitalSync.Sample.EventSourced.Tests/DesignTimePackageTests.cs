using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace VitalSync.Sample.EventSourced.Tests;

public sealed class DesignTimePackageTests
{
    private const string DesignPackage = "Microsoft.EntityFrameworkCore.Design";

    [Fact]
    public void TheInfrastructureProject_DoesNotReferenceTheDesignPackage()
    {
        var reference = FindDesignReference(
            ProjectPath("VitalSync.Sample.EventSourced.Infrastructure"));

        Assert.True(
            reference is null,
            $"'{DesignPackage}' is referenced by the Infrastructure project. A design-time package belongs to " +
            "the MigrationService, which is a leaf host: the Infrastructure project is referenced by the Api, " +
            "the MigrationService and the tests, so the package would travel into every one of them. Scaffold " +
            "with --project on Infrastructure and --startup-project on the MigrationService instead.");
    }

    [Fact]
    public void TheMigrationServiceProject_ReferencesTheDesignPackagePrivately()
    {
        var reference = FindDesignReference(
            ProjectPath("VitalSync.Sample.EventSourced.MigrationService"));

        Assert.NotNull(reference);
        Assert.Equal("all", reference.Attribute("PrivateAssets")?.Value, StringComparer.Ordinal);
        Assert.Null(reference.Attribute("IncludeAssets"));
    }

    private static XElement? FindDesignReference(string projectPath) =>
        XDocument.Load(projectPath)
            .Descendants("PackageReference")
            .FirstOrDefault(reference =>
                string.Equals(reference.Attribute("Include")?.Value, DesignPackage, StringComparison.Ordinal));

    private static string ProjectPath(string projectName, [CallerFilePath] string testFilePath = "") =>
        Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(testFilePath)!)!,
            projectName,
            $"{projectName}.csproj");
}
