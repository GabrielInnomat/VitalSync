using System.Reflection;
using BuildingBlocks.Application.Cqrs;

namespace BuildingBlocks.Application.Tests;

public sealed class PublicSurfaceTests
{
    private static readonly string[] PublishedApi =
    [
        "BuildingBlocks.Application.Cqrs.ICommand",
        "BuildingBlocks.Application.Cqrs.ICommand`1",
        "BuildingBlocks.Application.Cqrs.ICommandHandler`1",
        "BuildingBlocks.Application.Cqrs.ICommandHandler`2",
        "BuildingBlocks.Application.Cqrs.IPipelineBehavior`2",
        "BuildingBlocks.Application.Cqrs.IQuery`1",
        "BuildingBlocks.Application.Cqrs.IQueryHandler`2",
        "BuildingBlocks.Application.Cqrs.ISender",
        "BuildingBlocks.Application.Cqrs.RequestPipeline`1",
        "BuildingBlocks.Application.Cqrs.RequestPipelineContinuation`1",
        "BuildingBlocks.Application.DomainEvents.DomainEventMetadata",
        "BuildingBlocks.Application.DomainEvents.IProjectionHandler`1",
        "BuildingBlocks.Application.IntegrationEvents.IIntegrationEvent",
        "BuildingBlocks.Application.IntegrationEvents.IIntegrationEventMapper`1",
        "BuildingBlocks.Application.IntegrationEvents.IIntegrationEventPublisher",
        "BuildingBlocks.Application.IntegrationEvents.IIntegrationEventSink",
        "BuildingBlocks.Application.IntegrationEvents.IntegrationEventTopicAttribute",
        "BuildingBlocks.Application.Persistence.IRepository`2",
        "BuildingBlocks.Application.Persistence.IUnitOfWork",
        "BuildingBlocks.Application.ReadModels.IReadModelRebuilder`2",
        "BuildingBlocks.Application.Results.Failure",
        "BuildingBlocks.Application.Results.FailureCategory",
        "BuildingBlocks.Application.Results.Result",
        "BuildingBlocks.Application.Results.Result`1",
    ];

    [Fact]
    public void TheNamespaceLayoutAndVisibilityAreExactlyThePublishedApi()
    {
        var expected = PublishedApi.Order(StringComparer.Ordinal).ToArray();

        var actual = typeof(ISender).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheAssemblyExposesNoPublicField()
    {
        var fields = typeof(ISender).Assembly
            .GetExportedTypes()
            .Where(type => !type.IsEnum)
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(field => !field.IsLiteral)
            .Select(field => $"{field.DeclaringType?.FullName}.{field.Name}")
            .ToArray();

        Assert.Empty(fields);
    }
}
