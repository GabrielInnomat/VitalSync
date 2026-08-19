using System.Reflection;
using GaWeCodes.Thessera.Application.Cqrs;
using GaWeCodes.Thessera.Application.DomainEvents;
using GaWeCodes.Thessera.Application.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitalSync.Sample.StateStored.Application;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure;

namespace VitalSync.Sample.StateStored.Tests;

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

        Assert.IsType<Infrastructure.Read.WidgetPartAddedProjection>(
            scope.ServiceProvider.GetRequiredService<IProjectionHandler<WidgetPartAdded>>());

        Assert.IsType<Infrastructure.Read.WidgetPartQuantityChangedProjection>(
            scope.ServiceProvider.GetRequiredService<IProjectionHandler<WidgetPartQuantityChanged>>());

        Assert.IsType<Infrastructure.Read.WidgetPartRemovedProjection>(
            scope.ServiceProvider.GetRequiredService<IProjectionHandler<WidgetPartRemoved>>());

        var mapper = Assert.Single(scope.ServiceProvider.GetServices<IIntegrationEventMapper<WidgetCreated>>());
        Assert.IsType<Infrastructure.Integration.WidgetIntegrationEventMapper>(mapper);
    }

    [Fact]
    public void Infrastructure_RegistersHandlersAndTheReadStore()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateWidget, WidgetId>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<RenameWidget>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<AddWidgetPart, WidgetPartId>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<ChangeWidgetPartQuantity>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<RemoveWidgetPart, string>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQueryHandler<GetWidget, WidgetView>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IWidgetReadStore>());
    }

    [Fact]
    public void Infrastructure_SubscribesToNothing_SoNoEventCanTravelBackIntoThisContext()
    {
        var consumers = typeof(SampleStateStoredInfrastructure).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(method => method.Name is "Handle" or "HandleAsync" or "Consume" or "ConsumeAsync")
            .Where(method => method.GetParameters()
                .Any(parameter => typeof(IIntegrationEvent).IsAssignableFrom(parameter.ParameterType)))
            .Select(method => $"{method.DeclaringType?.FullName}.{method.Name}")
            .ToArray();

        Assert.True(
            consumers.Length == 0,
            "The state-stored sample publishes but never consumes, which is what makes the one-way flow in "
            + "CrossContextSmokeTests structural rather than a matter of timing. Found: "
            + string.Join(", ", consumers));
    }

    private static ServiceProvider BuildProvider()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddSampleStateStoredInfrastructure(
            "Host=localhost;Database=unused-write",
            "Host=localhost;Database=unused-read",
            new Uri("amqp://localhost"),
            "test-platform.integration-events");

        return builder.Services.BuildServiceProvider();
    }
}
