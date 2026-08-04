using AmbiguousRequestsFixture;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using Microsoft.Extensions.DependencyInjection;
using OrphanRequestsFixture;
using ValidHandlersFixture;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class HandlerStartupValidationTests
{
    [Fact]
    public void StartupValidation_AllHandlersRegistered_Passes()
    {
        using var provider = BuildProvider(options =>
            options.AddHandlersFrom(typeof(RegistrationCommand).Assembly));

        GetValidator(provider).Run();
    }

    [Fact]
    public void StartupValidation_CommandAndQueryWithoutHandlers_FailsNamingEveryRequestType()
    {
        using var provider = BuildProvider(options =>
            options.AddHandlersFrom(typeof(OrphanCommand).Assembly));

        var exception = Assert.Throws<InvalidOperationException>(() => GetValidator(provider).Run());

        Assert.Contains(nameof(OrphanCommand), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(OrphanQuery), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupValidation_RequestTypeWithMultipleResultContracts_FailsNamingTypeAndContracts()
    {
        using var provider = BuildProvider(options =>
            options.AddHandlersFrom(typeof(AmbiguousQuery).Assembly));

        var exception = Assert.Throws<InvalidOperationException>(() => GetValidator(provider).Run());

        Assert.Contains(nameof(AmbiguousQuery), exception.Message, StringComparison.Ordinal);
        Assert.Contains("IQuery<Int32>", exception.Message, StringComparison.Ordinal);
        Assert.Contains("IQuery<String>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupValidation_IsRegisteredByDefault()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is HandlerRegistrationCheck);
    }

    [Fact]
    public void StartupValidation_CannotBeTurnedOff()
    {
        var switches = typeof(BuildingBlocksOptions)
            .GetProperties()
            .Select(property => property.Name)
            .Where(name => name.Contains("Validate", StringComparison.Ordinal));

        Assert.Empty(switches);
    }

    private static ServiceProvider BuildProvider(Action<BuildingBlocksOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocks(configure);
        return services.BuildServiceProvider();
    }

    private static IStartupCheck GetValidator(ServiceProvider provider) =>
        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is HandlerRegistrationCheck);
}
