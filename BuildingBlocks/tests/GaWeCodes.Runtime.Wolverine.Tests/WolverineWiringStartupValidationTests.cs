using GaWeCodes.DependencyInjection;
using GaWeCodes.DependencyInjection.Validation;
using GaWeCodes.Persistence.StateStored;
using GaWeCodes.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Wolverine.Runtime;

namespace GaWeCodes.Tests;

public sealed class WolverineWiringStartupValidationTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;******";

    [Fact]
    public async Task PersistenceSelected_WithoutWolverine_FailsAtStartupNamingUseWolverine()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => GetValidator(provider).RunAsync(TestContext.Current.CancellationToken));

        Assert.Contains("UseWolverine", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PersistenceSelected_WithWolverineRuntime_Passes()
    {
        using var provider = BuildProvider(
            options => options.UseEfCorePersistence<TestDbContext>(ConnectionString),
            services => services.AddSingleton(Substitute.For<IWolverineRuntime>()));

        await GetValidator(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void NoWolverineCapabilitySelected_TheCheckIsNotRegisteredBecauseNothingCanNeedARuntime()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Empty(provider.GetServices<IStartupCheck>().OfType<WolverineRuntimeCheck>());
    }

    [Fact]
    public void ACapabilityNeedingWolverine_RegistersTheCheck()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

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
