using System.Globalization;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class EntityKeyFormatterTests
{
    [Fact]
    public void GetStreamKey_ComposesAggregateTypeNameAndKeyValue()
    {
        var streamKey = EntityKeyFormatter.GetStreamKey(typeof(Recipe), new RecipeId(42));

        Assert.Equal("Recipe/42", streamKey);
    }

    [Fact]
    public void GetStreamKey_IsCultureInvariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var german = EntityKeyFormatter.GetStreamKey(typeof(Recipe), new RecipeId(1234567));

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariant = EntityKeyFormatter.GetStreamKey(typeof(Recipe), new RecipeId(1234567));

            Assert.Equal(invariant, german);
            Assert.Equal("Recipe/1234567", german);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private sealed class Recipe;

    private readonly record struct RecipeId(int Value) : IEntityKey<int>
    {
        public bool IsEmpty => Value == 0;
    }
}
