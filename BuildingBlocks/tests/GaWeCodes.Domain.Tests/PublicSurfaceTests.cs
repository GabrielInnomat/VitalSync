using System.Reflection;

namespace GaWeCodes.Domain.Tests;

public sealed class PublicSurfaceTests
{
    private static readonly string[] PublishedApi =
    [
        "GaWeCodes.Domain.Aggregates.AggregateRoot`2",
        "GaWeCodes.Domain.Aggregates.AggregateState`2",
        "GaWeCodes.Domain.Aggregates.EventSourcedAggregateRoot`2",
        "GaWeCodes.Domain.Aggregates.IAggregateRoot`1",
        "GaWeCodes.Domain.Aggregates.IEventSourcedAggregateRoot`1",
        "GaWeCodes.Domain.Aggregates.IStateOwner",
        "GaWeCodes.Domain.Entities.Entity`2",
        "GaWeCodes.Domain.Entities.EntityBase`1",
        "GaWeCodes.Domain.Entities.EntityState`2",
        "GaWeCodes.Domain.Entities.IChildOwner`2",
        "GaWeCodes.Domain.Entities.IEntity`1",
        "GaWeCodes.Domain.Entities.IEntityKey",
        "GaWeCodes.Domain.Entities.IEntityKey`1",
        "GaWeCodes.Domain.Events.DomainEvent",
        "GaWeCodes.Domain.Events.IDomainEvent",
        "GaWeCodes.Domain.Events.IDomainEventOwner",
        "GaWeCodes.Domain.Events.IDomainEventRaiser",
        "GaWeCodes.Domain.Events.IHasDomainEvents",
        "GaWeCodes.Domain.IClock",
        "GaWeCodes.Domain.Naming.AggregateNameAttribute",
        "GaWeCodes.Domain.Naming.EventNameAttribute",
        "GaWeCodes.Domain.Naming.NameSegment",
        "GaWeCodes.Domain.Rules.BusinessRuleViolationException",
        "GaWeCodes.Domain.Rules.DomainValidationException",
        "GaWeCodes.Domain.Rules.IBusinessRule",
        "GaWeCodes.Domain.Rules.IDomainValidationRule",
        "GaWeCodes.Domain.Rules.RuleChecker",
        "GaWeCodes.Domain.Rules.RuleViolation",
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
