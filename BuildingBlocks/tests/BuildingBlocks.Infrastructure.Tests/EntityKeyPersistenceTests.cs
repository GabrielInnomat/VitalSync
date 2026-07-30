using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class EntityKeyPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task StronglyTypedKey_RoundTripsThroughPostgres()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        var options = new DbContextOptionsBuilder<RecipeContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        var id = new RecipeId(Guid.NewGuid());

        await using (var context = new RecipeContext(options))
        {
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            context.Recipes.Add(new RecipeRow { Id = id, Name = "Pasta" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new RecipeContext(options))
        {
            var found = await context.Recipes.FindAsync([id], TestContext.Current.CancellationToken);

            Assert.NotNull(found);
            Assert.Equal(id, found!.Id);
            Assert.Equal("Pasta", found.Name);
        }
    }

    private sealed class RecipeContext(DbContextOptions<RecipeContext> options) : DbContext(options)
    {
        public DbSet<RecipeRow> Recipes => Set<RecipeRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ApplyEntityKeyConversions();
    }

    private sealed class RecipeRow
    {
        public RecipeId Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private readonly record struct RecipeId(Guid Value) : IEntityKey<Guid>
    {
        public bool IsEmpty => Value == Guid.Empty;
    }
}
