using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class CompositionSingleCallTests
{
    [Fact]
    public void SecondCall_OnTheSameServiceCollection_Throws()
    {
        var services = new ServiceCollection();
        services.AddBuildingBlocks(_ => { });

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddBuildingBlocks(_ => { }));

        Assert.Contains("more than once", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SecondCall_OnTheHostBuilder_Throws()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddBuildingBlocks(_ => { });

        Assert.Throws<InvalidOperationException>(() => builder.AddBuildingBlocks(_ => { }));
    }

    [Fact]
    public void SecondCall_DoesNotOverwriteTheFirstRegistrationOfTheSharedState()
    {
        var services = new ServiceCollection();
        services.AddBuildingBlocks(options => options.AddPipelineBehavior(typeof(StrayBehavior<,>), 500));

        Assert.Throws<InvalidOperationException>(() => services.AddBuildingBlocks(_ => { }));

        using var provider = services.BuildServiceProvider();

        Assert.Equal(500, provider.GetRequiredService<PipelineBehaviorRegistry>().GetOrder(typeof(StrayBehavior<,>)));
    }

    [Fact]
    public void BehaviorRegisteredOnTheServiceCollection_FailsRegistration()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(StrayBehavior<,>));

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddBuildingBlocks(_ => { }));

        Assert.Contains(nameof(StrayBehavior<object, Result>), exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddPipelineBehavior", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BehaviorRegisteredByFactory_FailsRegistration()
    {
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<ProbeCommand, Result>>(_ => new StrayBehavior<ProbeCommand, Result>());

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddBuildingBlocks(_ => { }));

        Assert.Contains("factory-registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BehaviorRegisteredThroughOptions_PassesRegistration()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocks(options => options.AddPipelineBehavior(typeof(StrayBehavior<,>), 500));

        using var provider = services.BuildServiceProvider();

        Assert.Equal(500, provider.GetRequiredService<PipelineBehaviorRegistry>().GetOrder(typeof(StrayBehavior<,>)));
    }

    [Fact]
    public void GetOrder_ForAnUnknownBehavior_ThrowsInsteadOfCollidingWithLogging()
    {
        var registry = new PipelineBehaviorRegistry();

        var exception = Assert.Throws<InvalidOperationException>(
            () => registry.GetOrder(typeof(StrayBehavior<ProbeCommand, Result>)));

        Assert.Contains("no registered order", exception.Message, StringComparison.Ordinal);
    }

    private sealed record ProbeCommand : ICommand;

    private sealed class StrayBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TResponse : Result
    {
        public Task<TResponse> HandleAsync(
            TRequest request,
            RequestPipelineContinuation<TResponse> continuation,
            CancellationToken cancellationToken) => continuation(cancellationToken);
    }
}
