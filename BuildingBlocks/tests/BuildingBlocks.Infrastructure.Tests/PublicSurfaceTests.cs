using System.Reflection;
using BuildingBlocks.Infrastructure.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class PublicSurfaceTests
{
    private static readonly string[] IntendedApi =
    [
        "BuildingBlocks.Infrastructure.DependencyInjection.BuildingBlocksOptions",
        "BuildingBlocks.Infrastructure.DependencyInjection.HostApplicationBuilderExtensions",
        "BuildingBlocks.Infrastructure.DependencyInjection.InfrastructureProvisioning",
        "BuildingBlocks.Infrastructure.DependencyInjection.ServiceCollectionExtensions",
        "BuildingBlocks.Infrastructure.Persistence.EntityKeyModelBuilderExtensions",
        "BuildingBlocks.Infrastructure.ReadModels.EventSourcedReadModelRebuildRunner",
        "BuildingBlocks.Infrastructure.ReadModels.StateStoredReadModelRebuildRunner`1",
    ];

    private static readonly string[] IntendedTestingApi =
    [
        "BuildingBlocks.Infrastructure.Schema.PersistedSchema",
    ];

    private static readonly string[] RequiredByWolverineCodeGeneration =
    [
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelope",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelopeHandler",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventEnvelopeSerializer",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.DomainEventTypeRegistry",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionEnvelope",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionEnvelopeHandler",
        "BuildingBlocks.Infrastructure.Messaging.DomainEvents.ProjectionRunner",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.IIntegrationEventSinkFactory",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.IntegrationEventSourceContext",
        "BuildingBlocks.Infrastructure.Messaging.IntegrationEvents.OwnContextIntegrationEventFilter",
    ];

    [Fact]
    public void ThePublicSurface_IsExactlyTheIntendedApiPlusWhatCodeGenerationForces()
    {
        var expected = IntendedApi
            .Concat(IntendedTestingApi)
            .Concat(RequiredByWolverineCodeGeneration)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var actual = typeof(ServiceCollectionExtensions).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NoInfrastructureImplementationIsPublic()
    {
        var leaked = typeof(ServiceCollectionExtensions).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace is not null
                && (type.Namespace.Contains(".Persistence.", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Dispatching", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Events", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Time", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Wiring", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Registration", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Validation", StringComparison.Ordinal)))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(leaked);
    }

    [Fact]
    public void EveryTypeExemptedForCodeGeneration_IsActuallyReachableFromGeneratedCode()
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;

        foreach (var name in RequiredByWolverineCodeGeneration)
        {
            var type = assembly.GetType(name);
            Assert.NotNull(type);
            Assert.True(type.IsPublic, $"'{name}' is listed as code-generation exempt but is not public.");
        }
    }

    [Fact]
    public void TheAssemblyExposesNoPublicField()
    {
        var fields = typeof(ServiceCollectionExtensions).Assembly
            .GetExportedTypes()
            .Where(type => !type.IsEnum)
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(field => !field.IsLiteral)
            .Select(field => $"{field.DeclaringType?.FullName}.{field.Name}")
            .ToArray();

        Assert.Empty(fields);
    }
}
