using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection;
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

    [Fact(Skip = "Pending IMP-05: AddHandlersFrom uses AddScoped without de-duplication, so scanning the same assembly twice registers duplicate handlers instead of a single registration.")]
    public void AddHandlersFrom_CalledTwiceForSameAssembly_DoesNotDuplicateProjectionHandlers()
    {
        using var provider = BuildProvider(handlerScans: 2);

        Assert.Single(provider.GetServices<IProjectionHandler<RegistrationEvent>>());
    }

    private static ServiceProvider BuildProvider(int handlerScans)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocks(options =>
        {
            for (var scan = 0; scan < handlerScans; scan++)
            {
                options.AddHandlersFrom(typeof(HandlerRegistrationTests).Assembly);
            }
        });
        return services.BuildServiceProvider();
    }
}

internal sealed record RegistrationCommand : ICommand;

internal sealed record RegistrationQuery : IQuery<int>;

internal sealed record RegistrationEvent : DomainEvent;

internal sealed class RegistrationCommandHandler : ICommandHandler<RegistrationCommand>
{
    public Task<Result> Handle(RegistrationCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}

internal sealed class RegistrationQueryHandler : IQueryHandler<RegistrationQuery, int>
{
    public Task<Result<int>> Handle(RegistrationQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(0));
}

internal sealed class RegistrationProjectionHandler : IProjectionHandler<RegistrationEvent>
{
    public Task Handle(RegistrationEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class RegistrationMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent) => [];
}
