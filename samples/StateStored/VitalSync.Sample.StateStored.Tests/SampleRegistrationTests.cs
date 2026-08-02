using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitalSync.Sample.StateStored.Application;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure;

namespace VitalSync.Sample.StateStored.Tests;

// A projection that is never registered fails silently - the read model just stops updating, nothing throws
// and nothing logs. AddHandlersFrom scanning the Infrastructure assembly is what prevents that, and this is
// the only thing standing between "scanned" and "silently missing".
public sealed class SampleRegistrationTests
{
    [Fact]
    public void Infrastructure_RegistersProjectionsAndMapperByScanning()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<Infrastructure.Read.WidgetCreatedProjection>(
            scope.ServiceProvider.GetRequiredService<IProjectionHandler<WidgetCreated>>());

        Assert.IsType<Infrastructure.Read.WidgetRenamedProjection>(
            scope.ServiceProvider.GetRequiredService<IProjectionHandler<WidgetRenamed>>());

        var mapper = Assert.Single(scope.ServiceProvider.GetServices<IIntegrationEventMapper>());
        Assert.IsType<Infrastructure.Integration.WidgetIntegrationEventMapper>(mapper);
    }

    [Fact]
    public void Infrastructure_RegistersHandlersAndTheReadStore()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateWidget, WidgetId>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<RenameWidget>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQueryHandler<GetWidget, WidgetView>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IWidgetReadStore>());
    }

    // Nothing here connects: the registrations are what is under test, so placeholder connection strings
    // are enough.
    private static ServiceProvider BuildProvider()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddSampleStateStoredInfrastructure(
            "Host=localhost;Database=unused-write",
            "Host=localhost;Database=unused-read",
            new Uri("amqp://localhost"));

        return builder.Services.BuildServiceProvider();
    }
}
