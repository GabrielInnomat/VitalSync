using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Wolverine.Runtime;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class WolverineWiringStartupValidationTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;******";

    [Fact]
    public void PersistenceSelected_WithoutWolverine_FailsAtStartupNamingUseWolverine()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

        var exception = Assert.Throws<InvalidOperationException>(() => GetValidator(provider).Run());

        Assert.Contains("UseWolverine", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistenceSelected_WithWolverineRuntime_Passes()
    {
        using var provider = BuildProvider(
            options => options.UseEfCorePersistence<TestDbContext>(ConnectionString),
            services => services.AddSingleton(Substitute.For<IWolverineRuntime>()));

        GetValidator(provider).Run();
    }

    [Fact]
    public void NoWolverineCapabilitySelected_TheCheckPassesWithoutARuntime()
    {
        using var provider = BuildProvider(_ => { });

        GetValidator(provider).Run();
    }

    [Fact]
    public void TheCheckIsRegisteredEvenWhenNoCapabilityNeedsIt()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is WolverineRuntimeCheck);
    }

    private static ServiceProvider BuildProvider(
        Action<BuildingBlocksOptions> configure,
        Action<IServiceCollection>? registerExtras = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        registerExtras?.Invoke(services);
        services.AddBuildingBlocks(options =>
        {
            options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
            configure(options);
        });
        return services.BuildServiceProvider();
    }

    private static IStartupCheck GetValidator(ServiceProvider provider) =>
        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is WolverineRuntimeCheck);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
