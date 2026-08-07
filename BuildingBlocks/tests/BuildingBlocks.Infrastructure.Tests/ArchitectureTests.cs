using System.Reflection;
using System.Text.Json;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Infrastructure.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class ArchitectureTests
{
    private static readonly string[] ForbiddenInfrastructureDependencies =
    [
        "Microsoft.EntityFrameworkCore",
        "Marten",
        "Wolverine",
        "Npgsql",
        "JasperFx",
        "RabbitMQ",
    ];

    private static readonly Assembly Domain = typeof(DomainEvent).Assembly;
    private static readonly Assembly Application = typeof(Result).Assembly;

    [Fact]
    public void Domain_HasNoBuildingBlockOrInfrastructurePackageReferences()
    {
        var references = ReferencedAssemblyNames(Domain);

        Assert.DoesNotContain(references, name => name.StartsWith("BuildingBlocks", StringComparison.Ordinal));
        Assert.DoesNotContain(references, IsForbiddenInfrastructureDependency);
    }

    [Fact]
    public void Application_DependsOnlyOnDomain()
    {
        var references = ReferencedAssemblyNames(Application);

        Assert.Contains("BuildingBlocks.Domain", references);
        Assert.DoesNotContain("BuildingBlocks.Infrastructure", references);
        Assert.DoesNotContain(references, IsForbiddenInfrastructureDependency);
    }

    [Fact]
    public void Domain_DoesNotReferenceApplicationOrInfrastructure()
    {
        var references = ReferencedAssemblyNames(Domain);

        Assert.DoesNotContain("BuildingBlocks.Application", references);
        Assert.DoesNotContain("BuildingBlocks.Infrastructure", references);
    }

    [Fact]
    public void Infrastructure_ReferencesBothApplicationAndDomain()
    {
        var references = ReferencedAssemblyNames(typeof(ServiceCollectionExtensions).Assembly);

        Assert.Contains("BuildingBlocks.Application", references);
        Assert.Contains("BuildingBlocks.Domain", references);
    }

    [Fact]
    public void Domain_DeclaresNoInfrastructurePackage_NotEvenAnUnusedOne()
    {
        var packages = ResolvedPackages("BuildingBlocks/src/BuildingBlocks.Domain");

        Assert.DoesNotContain(packages, IsForbiddenInfrastructureDependency);
    }

    [Fact]
    public void Application_DeclaresNoInfrastructurePackage_NotEvenAnUnusedOne()
    {
        var packages = ResolvedPackages("BuildingBlocks/src/BuildingBlocks.Application");

        Assert.DoesNotContain(packages, IsForbiddenInfrastructureDependency);
    }

    [Fact]
    public void BuildingBlocks_NeverMentionsTheProductItServes()
    {
        var sources = Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot(), "BuildingBlocks", "src"),
            "*.*",
            SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => Path.GetExtension(path) is ".cs" or ".csproj" or ".json" or ".editorconfig");

        var offenders = sources
            .Where(path => File.ReadAllText(path).Contains("vitalsync", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Building Blocks must stay independent of VitalSync (ADR-0018): the product name may not appear "
            + $"under BuildingBlocks/src. Offending files: {string.Join(", ", offenders)}");
    }

    private static IReadOnlyCollection<string> ResolvedPackages(string projectDirectory)
    {
        var assets = Path.Combine(
            RepositoryRoot(),
            projectDirectory.Replace('/', Path.DirectorySeparatorChar),
            "obj",
            "project.assets.json");

        Assert.True(File.Exists(assets), $"'{assets}' does not exist; restore the solution before running this test.");

        using var document = JsonDocument.Parse(File.ReadAllText(assets));

        return document.RootElement.TryGetProperty("targets", out var targets)
            ?
            [
                .. targets.EnumerateObject()
                    .SelectMany(target => target.Value.EnumerateObject())
                    .Select(library => library.Name.Split('/')[0]),
            ]
            : [];
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VitalSync.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    private static IReadOnlyCollection<string> ReferencedAssemblyNames(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty)];

    private static bool IsForbiddenInfrastructureDependency(string name) =>
        Array.Exists(
            ForbiddenInfrastructureDependencies,
            forbidden => name.StartsWith(forbidden, StringComparison.Ordinal));
}
