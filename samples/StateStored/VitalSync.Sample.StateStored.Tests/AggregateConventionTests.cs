using System.Reflection;
using BuildingBlocks.Domain;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Tests;

// The reconstitution convention (ADR-0025 amendment) is enforced by the compiler at the point where a repository
// is injected - but only for aggregates that actually reach a repository. This scan closes the gap for the rest:
// a new aggregate that forgets IReconstitutable, or leaves its empty constructor public, fails here rather than
// months later when someone first tries to load it.
public sealed class AggregateConventionTests
{
    [Fact]
    public void EveryAggregate_ImplementsReconstitutable()
    {
        var aggregates = Aggregates();

        Assert.NotEmpty(aggregates);
        foreach (var aggregate in aggregates)
        {
            Assert.True(
                typeof(IReconstitutable<>).MakeGenericType(aggregate).IsAssignableFrom(aggregate),
                $"'{aggregate}' must implement IReconstitutable<{aggregate.Name}> so a repository can rehydrate it.");
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
                + "through its factory. Make it private and implement IReconstitutable explicitly.");
        }
    }

    private static List<Type> Aggregates() =>
        [.. typeof(Widget).Assembly.GetTypes().Where(type => !type.IsAbstract && DerivesFromAggregateRoot(type))];

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
