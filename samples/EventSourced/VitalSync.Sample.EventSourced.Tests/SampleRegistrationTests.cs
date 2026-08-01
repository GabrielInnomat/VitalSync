using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Domain;
using VitalSync.Sample.EventSourced.Infrastructure;

namespace VitalSync.Sample.EventSourced.Tests;

// A projection that is never registered fails silently - the read model just stops updating. The assembly
// scan is what prevents that, and this is the only thing standing between "scanned" and "silently missing".
public sealed class SampleRegistrationTests
{
    [Fact]
    public void Infrastructure_RegistersProjectionsAndMapperByScanning()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<Infrastructure.Read.GadgetCreatedProjection>(
            scope.ServiceProvider.GetRequiredService<IProjectionHandler<GadgetCreated>>());

        Assert.IsType<Infrastructure.Read.GadgetRenamedProjection>(
            scope.ServiceProvider.GetRequiredService<IProjectionHandler<GadgetRenamed>>());

        Assert.IsType<Infrastructure.Read.GadgetRetiredProjection>(
            scope.ServiceProvider.GetRequiredService<IProjectionHandler<GadgetRetired>>());

        var mapper = Assert.Single(scope.ServiceProvider.GetServices<IIntegrationEventMapper>());
        Assert.IsType<Infrastructure.Integration.GadgetIntegrationEventMapper>(mapper);
    }

    [Fact]
    public void Infrastructure_RegistersHandlersAndTheReadStore()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateGadget, GadgetId>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<RenameGadget>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<RetireGadget>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQueryHandler<GetGadget, GadgetView>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IGadgetReadStore>());
    }

    // Selecting Marten is what puts the event-sourced repository behind the one repository contract
    // (ADR-0026): the handlers above are written against IRepository and never learn which store answers.
    [Fact]
    public void Infrastructure_ResolvesTheRepositoryFromTheEventStore()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Gadget, GadgetId>>();

        Assert.StartsWith(
            "MartenEventSourcedRepository",
            repository.GetType().Name,
            StringComparison.Ordinal);
    }

    // Nothing here connects: the registrations are what is under test.
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSampleEventSourcedInfrastructure(
            "Host=localhost;Database=unused-write;Username=postgres;Password=postgres",
            "Host=localhost;Database=unused-read",
            new Uri("amqp://localhost"));

        return services.BuildServiceProvider();
    }
}
