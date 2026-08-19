using GaWeCodes.Core.Persistence;
using GaWeCodes.Domain.Entities;
using GaWeCodes.Persistence.EfCore.StateStored;
using GaWeCodes.Persistence.Marten;
using Marten;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace GaWeCodes.Tests;

public sealed class RepositoryEmptyIdentityGuardTests
{
    [Fact]
    public async Task EfCoreAddAsync_WithEmptyIdentity_Throws()
    {
        await using var context = new DbContext(
            new DbContextOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var repository = new EfCoreRepository<Counter, CounterId>(
            new WriteDbContextAccessor(context),
            new EfCoreAggregateTracker());
        var emptyHull = CreateEmptyHull<Counter>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(emptyHull, TestContext.Current.CancellationToken));

        Assert.Contains("has no identity", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EfCoreAddAsync_WithIdentity_DoesNotThrow()
    {
        await using var context = new GuardProbeContext(
            new DbContextOptionsBuilder<GuardProbeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var repository = new EfCoreRepository<Counter, CounterId>(
            new WriteDbContextAccessor(context),
            new EfCoreAggregateTracker());
        var counter = Counter.Create(new CounterId(Guid.NewGuid()));

        await repository.AddAsync(counter, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MartenAddAsync_WithEmptyIdentity_Throws()
    {
        var repository = new MartenEventSourcedRepository<Counter, CounterId>(
            Substitute.For<IDocumentSession>(), new MartenAggregateTracker());
        var emptyHull = CreateEmptyHull<Counter>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(emptyHull, TestContext.Current.CancellationToken));

        Assert.Contains("has no identity", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MartenAddAsync_WithIdentity_DoesNotThrow()
    {
        var repository = new MartenEventSourcedRepository<Counter, CounterId>(
            Substitute.For<IDocumentSession>(), new MartenAggregateTracker());
        var counter = Counter.Create(new CounterId(Guid.NewGuid()));

        await repository.AddAsync(counter, TestContext.Current.CancellationToken);
    }

    private static TAggregate CreateEmptyHull<TAggregate>()
        where TAggregate : class =>
        AggregateFactory.CreateEmpty<TAggregate>();

    private sealed class GuardProbeContext(DbContextOptions<GuardProbeContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CounterState>().HasKey(state => state.Id);
            modelBuilder.Entity<CounterState>()
                .Property(state => state.Id)
                .HasConversion(id => id.Value, value => new CounterId(value));
        }
    }
}
