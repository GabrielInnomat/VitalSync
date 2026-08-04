using System.Reflection;
using BuildingBlocks.Domain;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Tests;

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
    public void EveryAggregate_DeclaresItsStreamName()
    {
        var aggregates = Aggregates();

        Assert.NotEmpty(aggregates);
        foreach (var aggregate in aggregates)
        {
            Assert.True(
                aggregate.GetCustomAttribute<AggregateNameAttribute>(inherit: false) is not null,
                $"'{aggregate}' needs an [AggregateName]; renaming the class would otherwise orphan every "
                + "existing event stream (ADR-0030).");
        }
    }

    [Fact]
    public void EveryDomainEvent_DeclaresItsPersistedName()
    {
        var domainEvents = typeof(Gadget).Assembly.GetTypes()
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

    private static List<Type> Aggregates() =>
        [.. typeof(Gadget).Assembly.GetTypes().Where(type => !type.IsAbstract && DerivesFromAggregateRoot(type))];

    private static bool DerivesFromAggregateRoot(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<,>))
            {
                return true;
            }
        }

        return false;
    }
}
