using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class EntityKeyConversionTests
{
    [Fact]
    public void EntityKeyValueConverter_ConvertsBetweenKeyAndPrimitive()
    {
        var converter = new EntityKeyValueConverter<RecipeId, int>();

        Assert.Equal(5, converter.ConvertToProvider(new RecipeId(5)));
        Assert.Equal(new RecipeId(5), converter.ConvertFromProvider(5));
    }

    [Fact]
    public void ApplyEntityKeyConversions_ConfiguresConverterForStronglyTypedKeyProperties()
    {
        using var context = new SampleContext();

        var converter = context.Model
            .FindEntityType(typeof(RecipeRow))!
            .FindProperty(nameof(RecipeRow.Id))!
            .GetValueConverter();

        Assert.IsType<EntityKeyValueConverter<RecipeId, int>>(converter);
    }

    [Fact]
    public void ApplyEntityKeyConversions_LeavesAlreadyConfiguredPropertiesUntouched()
    {
        using var context = new SampleContext();

        var converter = context.Model
            .FindEntityType(typeof(TaggedRow))!
            .FindProperty(nameof(TaggedRow.Reference))!
            .GetValueConverter();

        Assert.IsType<CustomReferenceConverter>(converter);
    }

    private sealed class SampleContext : DbContext
    {
        public DbSet<RecipeRow> Recipes => Set<RecipeRow>();

        public DbSet<TaggedRow> Tagged => Set<TaggedRow>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseInMemoryDatabase(nameof(SampleContext));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaggedRow>()
                .Property(row => row.Reference)
                .HasConversion(new CustomReferenceConverter());

            modelBuilder.ApplyEntityKeyConversions();
        }
    }

    private sealed class RecipeRow
    {
        public RecipeId Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class TaggedRow
    {
        public int Id { get; set; }

        public RecipeId Reference { get; set; }
    }

    private sealed class CustomReferenceConverter() : ValueConverter<RecipeId, int>(
        key => key.Value,
        value => new RecipeId(value));

    private readonly record struct RecipeId(int Value) : IEntityKey<int>
    {
        public bool IsEmpty => Value == 0;
    }
}
