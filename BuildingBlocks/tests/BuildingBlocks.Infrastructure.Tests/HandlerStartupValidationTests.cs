using AmbiguousRequestsFixture;
using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrphanRequestsFixture;
using ValidHandlersFixture;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class HandlerStartupValidationTests
{
    [Fact]
    public async Task StartupValidation_AllHandlersRegistered_Passes()
    {
        using var provider = BuildProvider(options =>
            options.AddHandlersFrom(typeof(RegistrationCommand).Assembly));

        var validator = GetValidator(provider);

        await validator.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartupValidation_CommandAndQueryWithoutHandlers_FailsNamingEveryRequestType()
    {
        using var provider = BuildProvider(options =>
            options.AddHandlersFrom(typeof(OrphanCommand).Assembly));

        var validator = GetValidator(provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));

        Assert.Contains(nameof(OrphanCommand), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(OrphanQuery), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartupValidation_RequestTypeWithMultipleResultContracts_FailsNamingTypeAndContracts()
    {
        using var provider = BuildProvider(options =>
            options.AddHandlersFrom(typeof(AmbiguousQuery).Assembly));

        var validator = GetValidator(provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));

        Assert.Contains(nameof(AmbiguousQuery), exception.Message, StringComparison.Ordinal);
        Assert.Contains("IQuery<Int32>", exception.Message, StringComparison.Ordinal);
        Assert.Contains("IQuery<String>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupValidation_IsRegisteredByDefault()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Single(
            provider.GetServices<IHostedService>(),
            service => service is HandlerRegistrationStartupValidator);
    }

    [Fact]
    public void StartupValidation_OptedOut_IsNotRegistered()
    {
        using var provider = BuildProvider(options => options.ValidateHandlersOnStart = false);

        Assert.DoesNotContain(
            provider.GetServices<IHostedService>(),
            service => service is HandlerRegistrationStartupValidator);
    }

    private static ServiceProvider BuildProvider(Action<BuildingBlocksOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocks(configure);
        return services.BuildServiceProvider();
    }

    private static IHostedService GetValidator(ServiceProvider provider) =>
        Assert.Single(
            provider.GetServices<IHostedService>(),
            service => service is HandlerRegistrationStartupValidator);
}
