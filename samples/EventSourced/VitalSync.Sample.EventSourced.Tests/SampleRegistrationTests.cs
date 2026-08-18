using GaWeCodes.Application.Cqrs;
using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Domain;
using VitalSync.Sample.EventSourced.Infrastructure;

namespace VitalSync.Sample.EventSourced.Tests;

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

        var mapper = Assert.Single(scope.ServiceProvider.GetServices<IIntegrationEventMapper<GadgetRetired>>());
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

    private static ServiceProvider BuildProvider()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddSampleEventSourcedInfrastructure(
            "Host=localhost;Database=unused-write;Username=postgres;Password=postgres",
            "Host=localhost;Database=unused-read",
            new Uri("amqp://localhost"),
            "test-platform.integration-events");

        return builder.Services.BuildServiceProvider();
    }
}
