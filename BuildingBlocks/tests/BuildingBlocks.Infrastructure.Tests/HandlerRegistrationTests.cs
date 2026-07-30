using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Tests.ConflictingHandlers;
using BuildingBlocks.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class HandlerRegistrationTests
{
    [Fact]
    public void AddHandlersFrom_RegistersCommandQueryProjectionHandlersAndMappers()
    {
        using var provider = BuildProvider(handlerScans: 1);

        Assert.Single(provider.GetServices<ICommandHandler<RegistrationCommand>>());
        Assert.Single(provider.GetServices<IQueryHandler<RegistrationQuery, int>>());
        Assert.Single(provider.GetServices<IProjectionHandler<RegistrationEvent>>());
        Assert.Contains(provider.GetServices<IIntegrationEventMapper>(), mapper => mapper is RegistrationMapper);
    }

    [Fact]
    public void AddHandlersFrom_CalledTwiceForSameAssembly_DoesNotDuplicateProjectionHandlers()
    {
        using var provider = BuildProvider(handlerScans: 2);

        Assert.Single(provider.GetServices<IProjectionHandler<RegistrationEvent>>());
    }

    [Fact]
    public void AddHandlersFrom_CalledTwiceForSameAssembly_DoesNotDuplicateIntegrationEventMappers()
    {
        using var provider = BuildProvider(handlerScans: 2);

        Assert.Single(provider.GetServices<IIntegrationEventMapper>(), mapper => mapper is RegistrationMapper);
    }

    [Fact]
    public void AddHandlersFrom_CalledTwiceForSameAssembly_DoesNotThrowForSingleHandlers()
    {
        using var provider = BuildProvider(handlerScans: 2);

        Assert.Single(provider.GetServices<ICommandHandler<RegistrationCommand>>());
        Assert.Single(provider.GetServices<IQueryHandler<RegistrationQuery, int>>());
    }

    [Fact]
    public void AddHandlersFrom_TwoDifferentHandlersForSameCommand_Throws()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBuildingBlocks(options =>
                options.AddHandlersFrom(typeof(ConflictingCommand).Assembly)));

        Assert.Contains(nameof(FirstConflictingCommandHandler), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(SecondConflictingCommandHandler), exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildProvider(int handlerScans)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocks(options =>
        {
            for (var scan = 0; scan < handlerScans; scan++)
            {
                options.AddHandlersFrom(typeof(RegistrationCommand).Assembly);
            }
        });
        return services.BuildServiceProvider();
    }
}
