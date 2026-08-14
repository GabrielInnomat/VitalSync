using System.Globalization;
using BuildingBlocks.Domain.Entities;
using BuildingBlocks.Domain.Naming;
using BuildingBlocks.Infrastructure.Persistence;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class EntityKeyFormatterTests
{
    [Fact]
    public void GetStreamKey_ComposesTheAggregateNameAndTheKeyValue()
    {
        var streamKey = EntityKeyFormatter.GetStreamKey(
            EntityKeyFormatter.GetAggregateName(typeof(Recipe)),
            EntityKeyFormatter.GetKeyValue(new RecipeId(42)));

        Assert.Equal("recipe/42", streamKey);
    }

    [Fact]
    public void GetAggregateName_DoesNotFollowTheClrTypeName()
    {
        Assert.Equal("recipe", EntityKeyFormatter.GetAggregateName(typeof(Recipe)));
        Assert.Equal("recipe", EntityKeyFormatter.GetAggregateName(typeof(RenamedRecipe)));
    }

    [Fact]
    public void GetAggregateName_WithoutTheAttribute_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => EntityKeyFormatter.GetAggregateName(typeof(UnnamedAggregate)));

        Assert.Contains("AggregateName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetKeyValue_IsCultureInvariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var german = EntityKeyFormatter.GetKeyValue(new RecipeId(1234567));

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariant = EntityKeyFormatter.GetKeyValue(new RecipeId(1234567));

            Assert.Equal(invariant, german);
            Assert.Equal("1234567", german);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [AggregateName("recipe")]
    private sealed class Recipe;

    [AggregateName("recipe")]
    private sealed class RenamedRecipe;

    private sealed class UnnamedAggregate;

    private readonly record struct RecipeId(int Value) : IEntityKey<int>
    {
        public bool IsEmpty => Value == 0;
    }
}
