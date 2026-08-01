using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

// EF Core maps the aggregate's state, not the aggregate: the repository rehydrates an aggregate around a
// loaded state and the unit of work copies the current state back onto the tracked entity at commit. That
// copy is the whole reason an update reaches the database at all — state objects are immutable, so the
// instance EF tracks goes stale the moment an event is applied. These tests exercise both directions.
[Collection(PostgreSqlCollection.Name)]
public sealed class EfCoreAggregateRoundTripTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task LoadedAggregate_KeepsItsIdentityAndPersistsSubsequentChanges()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        using var host = await StartHostAsync();
        var id = Guid.NewGuid();

        await SendAsync(host, new StartFlushProbe(id));
        await SendAsync(host, new RenameFlushProbe(id, "renamed"));

        using var scope = host.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<FlushProbe, FlushProbeId>>();
        var reloaded = await repository.GetByIdAsync(new FlushProbeId(id), TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);

        // The identity survives the round trip even though the aggregate itself is never mapped: Id is
        // derived from the state, and the state is what was stored.
        Assert.Equal(id, reloaded!.Id.Value);
        Assert.Equal("renamed", reloaded.Name);

        // A rehydrated aggregate carries no uncommitted events - Restore is not a state change.
        Assert.Empty(reloaded.DomainEvents);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MissingAggregate_ReturnsNull()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        using var host = await StartHostAsync();

        using var scope = host.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<FlushProbe, FlushProbeId>>();

        var missing = await repository.GetByIdAsync(
            new FlushProbeId(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.Null(missing);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private static async Task SendAsync(IHost host, ICommand command)
    {
        using var scope = host.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(command, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
    }

    private async Task<IHost> StartHostAsync()
    {
        var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBuildingBlocks(options =>
                    options.UseEfCorePersistence<FlushProbeContext>(fixture.ConnectionString));
                services.AddScoped<ICommandHandler<StartFlushProbe>, StartFlushProbeHandler>();
                services.AddScoped<ICommandHandler<RenameFlushProbe>, RenameFlushProbeHandler>();
            })
            .UseWolverine(options =>
            {
                options.Durability.Mode = DurabilityMode.Solo;
                options.UseBuildingBlocksEfCorePersistence(fixture.ConnectionString);
                options.ApplicationAssembly = typeof(DomainEventEnvelopeHandler).Assembly;
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<FlushProbeContext>().Database.ExecuteSqlRawAsync(
            "create table if not exists flush_probe_rows (id uuid primary key, name text not null)",
            TestContext.Current.CancellationToken);

        return host;
    }
}
