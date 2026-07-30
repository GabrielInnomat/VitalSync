using System.Reflection;
using BuildingBlocks.Application;
using BuildingBlocks.Domain;
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

    private static IReadOnlyCollection<string> ReferencedAssemblyNames(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty)];

    private static bool IsForbiddenInfrastructureDependency(string name) =>
        Array.Exists(
            ForbiddenInfrastructureDependencies,
            forbidden => name.StartsWith(forbidden, StringComparison.Ordinal));
}
