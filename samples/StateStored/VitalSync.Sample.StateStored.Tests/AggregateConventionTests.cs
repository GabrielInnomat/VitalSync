using System.Reflection;
using BuildingBlocks.Domain;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Tests;

public sealed class AggregateConventionTests
{
    [Fact]
    public void EveryAggregate_HasAParameterlessConstructorForRehydration()
    {
        var aggregates = Aggregates();

        Assert.NotEmpty(aggregates);
        foreach (var aggregate in aggregates)
        {
            Assert.True(
                aggregate.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    Type.EmptyTypes) is not null,
                $"'{aggregate}' has no parameterless constructor, so no repository can reconstitute it. "
                + "Building Blocks verifies this at host startup; this test catches it earlier.");
        }
    }

    [Fact]
    public void NoAggregate_ExposesAPublicParameterlessConstructor()
    {
        var aggregates = Aggregates();

        Assert.NotEmpty(aggregates);
        foreach (var aggregate in aggregates)
        {
            Assert.True(
                aggregate.GetConstructor(Type.EmptyTypes) is null,
                $"'{aggregate}' exposes a public parameterless constructor, so it can be created without going "
                + "through its factory. Keep the parameterless constructor private.");
        }
    }

    [Fact]
    public void EveryAggregate_DeclaresItsPersistedName()
    {
        var aggregates = Aggregates();

        Assert.NotEmpty(aggregates);
        foreach (var aggregate in aggregates)
        {
            Assert.True(
                aggregate.GetCustomAttribute<AggregateNameAttribute>(inherit: false) is not null,
                $"'{aggregate}' needs an [AggregateName]; the class name is not a persistence contract (ADR-0030).");
        }
    }

    [Fact]
    public void EveryDomainEvent_DeclaresItsPersistedName()
    {
        var domainEvents = typeof(Widget).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(type))
            .ToList();

        Assert.NotEmpty(domainEvents);
        foreach (var domainEvent in domainEvents)
        {
            Assert.True(
                domainEvent.GetCustomAttribute<EventNameAttribute>(inherit: false) is not null,
                $"'{domainEvent}' needs an [EventName]; the class name is not a persistence contract (ADR-0030).");
        }
    }

    [Fact]
    public void NoChildEntity_IsConstructableFromOutsideItsAggregate()
    {
        var children = ChildEntities();

        Assert.NotEmpty(children);
        foreach (var child in children)
        {
            Assert.True(
                child.GetConstructors().Length == 0,
                $"'{child}' exposes a public constructor, so a child hull can be built without its root and would "
                + "have no channel to raise through (ADR-0032). Keep the constructor internal.");
        }
    }

    private static List<Type> Aggregates() =>
        [.. typeof(Widget).Assembly.GetTypes().Where(type => !type.IsAbstract && DerivesFrom(type, typeof(AggregateRoot<,>)))];

    private static List<Type> ChildEntities() =>
        [.. typeof(Widget).Assembly.GetTypes().Where(type => !type.IsAbstract && DerivesFrom(type, typeof(Entity<,>)))];

    private static bool DerivesFrom(Type type, Type openGeneric)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGeneric)
            {
                return true;
            }
        }

        return false;
    }
}
