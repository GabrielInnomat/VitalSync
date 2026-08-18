using GaWeCodes.Application.Cqrs;
using GaWeCodes.Application.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Infrastructure;
using Wolverine;
using Wolverine.Runtime.Routing;

namespace VitalSync.Sample.EventSourced.Tests;

public sealed class SubscriptionDiscoveryTests
{
    [Fact]
    public async Task WithTheConsumerAssemblyIncluded_TheEventReachesACommand()
    {
        var sender = Substitute.For<ISender>();
        sender.SendAsync(Arg.Any<ICommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var widgetId = Guid.NewGuid();

        using var host = await BuildHost(sender, includeConsumerAssembly: true);

        await host.Services.GetRequiredService<IMessageBus>()
            .InvokeAsync(new WidgetCreatedIntegrationEvent(widgetId, "mirrored", Guid.NewGuid(), DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        await sender.Received(1).SendAsync(
            Arg.Is<ICommand>(command => ((MirrorWidget)command).WidgetId == widgetId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithoutIt_WolverineHasNoHandlerAtAll()
    {
        using var host = await BuildHost(Substitute.For<ISender>(), includeConsumerAssembly: false);

        var thrown = await Record.ExceptionAsync(() =>
            host.Services.GetRequiredService<IMessageBus>()
                .InvokeAsync(new WidgetCreatedIntegrationEvent(Guid.NewGuid(), "ignored", Guid.NewGuid(), DateTimeOffset.UtcNow), TestContext.Current.CancellationToken));

        var missingHandler = Assert.IsType<IndeterminateRoutesException>(thrown);
        Assert.Contains(
            nameof(WidgetCreatedIntegrationEvent),
            missingHandler.Message,
            StringComparison.Ordinal);
    }

    private static Task<IHost> BuildHost(ISender sender, bool includeConsumerAssembly) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddSingleton(sender))
            .UseWolverine(options =>
            {
                if (includeConsumerAssembly)
                {
                    options.Discovery.IncludeAssembly(typeof(SampleEventSourcedInfrastructure).Assembly);
                }
            })
            .StartAsync();
}
