using System.Reflection;
using GaWeCodes.Application.Cqrs;

namespace GaWeCodes.Application.Tests;

public sealed class PublicSurfaceTests
{
    private static readonly string[] PublishedApi =
    [
        "GaWeCodes.Application.Cqrs.ICommand",
        "GaWeCodes.Application.Cqrs.ICommand`1",
        "GaWeCodes.Application.Cqrs.ICommandHandler`1",
        "GaWeCodes.Application.Cqrs.ICommandHandler`2",
        "GaWeCodes.Application.Cqrs.IPipelineBehavior`2",
        "GaWeCodes.Application.Cqrs.IQuery`1",
        "GaWeCodes.Application.Cqrs.IQueryHandler`2",
        "GaWeCodes.Application.Cqrs.ISender",
        "GaWeCodes.Application.Cqrs.RequestPipeline`1",
        "GaWeCodes.Application.Cqrs.RequestPipelineContinuation`1",
        "GaWeCodes.Application.DomainEvents.DomainEventMetadata",
        "GaWeCodes.Application.DomainEvents.IProjectionHandler`1",
        "GaWeCodes.Application.IntegrationEvents.IIntegrationEvent",
        "GaWeCodes.Application.IntegrationEvents.IIntegrationEventMapper`1",
        "GaWeCodes.Application.IntegrationEvents.IIntegrationEventPublisher",
        "GaWeCodes.Application.IntegrationEvents.IIntegrationEventSink",
        "GaWeCodes.Application.IntegrationEvents.IntegrationEventTopicAttribute",
        "GaWeCodes.Application.Persistence.IRepository`2",
        "GaWeCodes.Application.Persistence.IUnitOfWork",
        "GaWeCodes.Application.ReadModels.IReadModelRebuilder`2",
        "GaWeCodes.Application.Results.Failure",
        "GaWeCodes.Application.Results.FailureCategory",
        "GaWeCodes.Application.Results.Result",
        "GaWeCodes.Application.Results.Result`1",
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
