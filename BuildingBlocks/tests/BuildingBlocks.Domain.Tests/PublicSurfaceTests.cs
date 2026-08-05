using System.Reflection;

namespace BuildingBlocks.Domain.Tests;

public sealed class PublicSurfaceTests
{
    private static readonly string[] PublishedApi =
    [
        "BuildingBlocks.Domain.Aggregates.AggregateRoot`2",
        "BuildingBlocks.Domain.Aggregates.AggregateState`2",
        "BuildingBlocks.Domain.Aggregates.EventSourcedAggregateRoot`2",
        "BuildingBlocks.Domain.Aggregates.IAggregateRoot`1",
        "BuildingBlocks.Domain.Aggregates.IEventSourcedAggregateRoot`1",
        "BuildingBlocks.Domain.Aggregates.IStateOwner",
        "BuildingBlocks.Domain.Entities.Entity`2",
        "BuildingBlocks.Domain.Entities.EntityBase`1",
        "BuildingBlocks.Domain.Entities.EntityState`2",
        "BuildingBlocks.Domain.Entities.IEntity`1",
        "BuildingBlocks.Domain.Entities.IEntityKey",
        "BuildingBlocks.Domain.Entities.IEntityKey`1",
        "BuildingBlocks.Domain.Events.DomainEvent",
        "BuildingBlocks.Domain.Events.IDomainEvent",
        "BuildingBlocks.Domain.Events.IDomainEventOwner",
        "BuildingBlocks.Domain.Events.IDomainEventRaiser",
        "BuildingBlocks.Domain.Events.IHasDomainEvents",
        "BuildingBlocks.Domain.IClock",
        "BuildingBlocks.Domain.Naming.AggregateNameAttribute",
        "BuildingBlocks.Domain.Naming.EventNameAttribute",
        "BuildingBlocks.Domain.Naming.KebabCase",
        "BuildingBlocks.Domain.Rules.BusinessRuleViolationException",
        "BuildingBlocks.Domain.Rules.DomainValidationException",
        "BuildingBlocks.Domain.Rules.IBusinessRule",
        "BuildingBlocks.Domain.Rules.IDomainValidationRule",
        "BuildingBlocks.Domain.Rules.RuleChecker",
    ];

    [Fact]
    public void TheNamespaceLayoutAndVisibilityAreExactlyThePublishedApi()
    {
        var expected = PublishedApi.Order(StringComparer.Ordinal).ToArray();

        var actual = typeof(IClock).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TheAssemblyExposesNoPublicField()
    {
        var fields = typeof(IClock).Assembly
            .GetExportedTypes()
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(field => !field.IsLiteral)
            .Select(field => $"{field.DeclaringType?.FullName}.{field.Name}")
            .ToArray();

        Assert.Empty(fields);
    }
}
