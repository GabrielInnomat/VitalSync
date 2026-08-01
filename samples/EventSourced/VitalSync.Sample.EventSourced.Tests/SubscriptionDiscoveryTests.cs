using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Infrastructure;
using Wolverine;

namespace VitalSync.Sample.EventSourced.Tests;

// The failure mode this guards against is the worst kind: an integration event whose handler was not
// discovered is not an error. Wolverine reports that it found no handler, marks the envelope handled and
// moves on - the queue drains, the dead-letter queue stays empty, and nothing happens. Only the missing row
// in the read model gives it away, much later.
public sealed class SubscriptionDiscoveryTests
{
    [Fact]
    public async Task WithTheConsumerAssemblyIncluded_TheEventReachesACommand()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ICommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var widgetId = Guid.NewGuid();

        using var host = await BuildHost(sender, includeConsumerAssembly: true);

        await host.Services.GetRequiredService<IMessageBus>()
            .InvokeAsync(new WidgetCreatedIntegrationEvent(widgetId, "mirrored"), TestContext.Current.CancellationToken);

        await sender.Received(1).Send(
            Arg.Is<ICommand>(command => ((MirrorWidget)command).WidgetId == widgetId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithoutIt_WolverineHasNoHandlerAtAll()
    {
        // Wolverine scans the entry assembly only, and here that is the test assembly. Pinning the negative
        // case keeps the Discovery line in Program.cs from looking like something that can be tidied away.
        using var host = await BuildHost(Substitute.For<ISender>(), includeConsumerAssembly: false);

        var thrown = await Record.ExceptionAsync(() =>
            host.Services.GetRequiredService<IMessageBus>()
                .InvokeAsync(new WidgetCreatedIntegrationEvent(Guid.NewGuid(), "ignored"), TestContext.Current.CancellationToken));

        Assert.NotNull(thrown);
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
