using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Domain;
using VitalSync.Sample.EventSourced.Infrastructure.Read;

namespace VitalSync.Sample.EventSourced.Infrastructure;

public static class SampleEventSourcedInfrastructure
{
    public const string ContextName = SampleContexts.EventSourced;

    public static IHostApplicationBuilder AddSampleEventSourcedInfrastructure(
        this IHostApplicationBuilder builder,
        string writeConnectionString,
        string readConnectionString,
        Uri rabbitMqUri,
        string exchangeName,
        InfrastructureProvisioning provisioning = InfrastructureProvisioning.Never)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        builder.AddBuildingBlocks(options =>
        {
            options.AddHandlersFrom(typeof(CreateGadget).Assembly);
            options.AddHandlersFrom(typeof(SampleEventSourcedInfrastructure).Assembly);
            options.AddDomainEventsFrom(typeof(Gadget).Assembly);

            options.UseMartenEventSourcing(writeConnectionString);
            options.UseWolverineMessaging(rabbitMqUri, exchangeName, ContextName);
            options.ProvisionInfrastructure(provisioning);

            options.SubscribeToIntegrationEvents(
                "eventsourced.integration-events",
                typeof(SampleEventSourcedInfrastructure).Assembly,
                SampleContexts.StateStored + ".*");
        });

        services.AddDbContext<GadgetReadDbContext>(builder => builder.UseNpgsql(readConnectionString));

        services.AddScoped<IGadgetReadStore, GadgetReadStore>();

        return builder;
    }
}
