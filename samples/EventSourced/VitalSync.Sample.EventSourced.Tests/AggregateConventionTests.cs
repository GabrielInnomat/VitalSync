using BuildingBlocks.Domain;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Tests;

// Same convention scan as the state-stored sample: reconstitution is written identically on both persistence
// paths (ADR-0025), so the check that guards it is identical too.
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
