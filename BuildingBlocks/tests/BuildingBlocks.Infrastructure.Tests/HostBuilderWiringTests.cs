using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.EntityFrameworkCore;
using Wolverine.RDBMS;
using Wolverine.Runtime;

namespace BuildingBlocks.Infrastructure.Tests;

// The host names its write database exactly once, in UseEfCorePersistence. Before the ADR-0027 amendment it had
// to repeat the same string in its own UseWolverine call, and nothing compared the two: two typos apart, the
// outbox sat in a different database than the aggregates and the ADR-0022 atomicity guarantee was silently gone.
// These tests pin that the second mention no longer exists - Building Blocks configures Wolverine itself.
public sealed class HostBuilderWiringTests
{
    private const string WriteConnectionString = "Host=localhost;Database=wiring-write;Username=test;Password=test";

    private static readonly Uri RabbitMqUri = new("amqp://guest:guest@localhost:5672");

    [Fact]
    public void EfCoreSelection_PointsWolverinesMessageStoreAtTheSelectedWriteDatabase()
    {
        var builder = BuildHost(options => options.UseEfCorePersistence<TestDbContext>(WriteConnectionString));

        var settings = Assert.Single(
            builder.Services
                .Select(descriptor => descriptor.ImplementationInstance)
                .OfType<DatabaseSettings>());

        Assert.Equal(WriteConnectionString, settings.ConnectionString);
    }

    [Fact]
    public void EfCoreSelection_AppliesTheEntityFrameworkCoreTransactionalMiddleware()
    {
        var builder = BuildHost(options => options.UseEfCorePersistence<TestDbContext>(WriteConnectionString));

        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IDbContextOutbox));
    }

    // Marten supplies its own message store through IntegrateWithWolverine, so the PostgreSQL-backed store of the
    // EF Core path must not be applied on top of it.
    [Fact]
    public void MartenSelection_ConfiguresWolverineWithoutThePostgresqlMessageStore()
    {
        var builder = BuildHost(options => options.UseMartenEventSourcing(WriteConnectionString));

        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IWolverineRuntime));
        Assert.DoesNotContain(
            builder.Services,
            descriptor => descriptor.ImplementationInstance is DatabaseSettings);
    }

    [Fact]
    public void MessagingSelection_ConfiguresWolverine()
    {
        var builder = BuildHost(options => options.UseWolverineMessaging(RabbitMqUri));

        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IWolverineRuntime));
    }

    // A host that selects nothing needing the outbox gets no Wolverine runtime forced on it - the same rule the
    // startup validator encodes from the other side.
    [Fact]
    public void NoCapabilitySelected_LeavesWolverineUnconfigured()
    {
        var builder = BuildHost(_ => { });

        Assert.DoesNotContain(builder.Services, descriptor => descriptor.ServiceType == typeof(IWolverineRuntime));
    }

    [Fact]
    public void HostSpecificWolverineConfiguration_IsApplied()
    {
        var builder = Host.CreateApplicationBuilder();
        var applied = false;

        builder.AddBuildingBlocks(
            options => options.UseEfCorePersistence<TestDbContext>(WriteConnectionString),
            _ => applied = true);

        Assert.True(applied);
    }

    // The callback is also the only reason to bring up Wolverine for a host that selected no Building Block
    // capability at all; without it there would be nothing to configure.
    [Fact]
    public void HostSpecificWolverineConfiguration_WithoutAnyCapability_StillConfiguresWolverine()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddBuildingBlocks(_ => { }, _ => { });

        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IWolverineRuntime));
    }

    private static HostApplicationBuilder BuildHost(Action<BuildingBlocksOptions> configure)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddBuildingBlocks(configure);
        return builder;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
