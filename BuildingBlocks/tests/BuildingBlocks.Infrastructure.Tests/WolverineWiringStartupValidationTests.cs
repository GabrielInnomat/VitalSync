using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Wolverine.Runtime;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class WolverineWiringStartupValidationTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";

    [Fact]
    public async Task PersistenceSelected_WithoutWolverine_FailsAtStartupNamingUseWolverine()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

        var validator = GetValidator(provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));

        Assert.Contains("UseWolverine", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PersistenceSelected_WithWolverineRuntime_Starts()
    {
        using var provider = BuildProvider(
            options => options.UseEfCorePersistence<TestDbContext>(ConnectionString),
            services => services.AddSingleton(Substitute.For<IWolverineRuntime>()));

        var validator = GetValidator(provider);

        await validator.StartAsync(CancellationToken.None);
    }

    [Fact]
    public void NoWolverineCapabilitySelected_ValidatorIsNotRegistered()
    {
        using var provider = BuildProvider(_ => { });

        Assert.DoesNotContain(
            provider.GetServices<IHostedService>(),
            service => service is WolverineWiringStartupValidator);
    }

    [Fact]
    public void OptedOut_ValidatorIsNotRegistered()
    {
        using var provider = BuildProvider(options =>
        {
            options.UseEfCorePersistence<TestDbContext>(ConnectionString);
            options.ValidateWolverineOnStart = false;
        });

        Assert.DoesNotContain(
            provider.GetServices<IHostedService>(),
            service => service is WolverineWiringStartupValidator);
    }

    private static ServiceProvider BuildProvider(
        Action<BuildingBlocksOptions> configure,
        Action<IServiceCollection>? registerExtras = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        registerExtras?.Invoke(services);
        services.AddBuildingBlocks(configure);
        return services.BuildServiceProvider();
    }

    private static IHostedService GetValidator(ServiceProvider provider) =>
        Assert.Single(
            provider.GetServices<IHostedService>(),
            service => service is WolverineWiringStartupValidator);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
