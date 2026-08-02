using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Infrastructure.Read;

namespace VitalSync.Sample.EventSourced.Infrastructure;

public static class SampleEventSourcedInfrastructure
{
    /// <summary>
    /// Registers everything the sample service needs; the host adds nothing on top.
    /// </summary>
    public static IHostApplicationBuilder AddSampleEventSourcedInfrastructure(
        this IHostApplicationBuilder builder,
        string writeConnectionString,
        string readConnectionString,
        Uri rabbitMqUri)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        builder.AddBuildingBlocks(options =>
        {
            // Two assemblies, same reason as on the state-stored side: handlers in Application, projections
            // and the integration-event mapper in Infrastructure.
            options.AddHandlersFrom(typeof(CreateGadget).Assembly);
            options.AddHandlersFrom(typeof(SampleEventSourcedInfrastructure).Assembly);

            // The one line that differs from the state-stored service. Marten brings its own message store
            // through IntegrateWithWolverine, so nothing extra is applied for this path when Building Blocks
            // configures Wolverine - this is where ADR-0027 always held without an exception.
            options.UseMartenEventSourcing(writeConnectionString);
            options.UseWolverineMessaging(rabbitMqUri);

            // The subscribing half. The assembly is this one and not the Application assembly on purpose:
            // Wolverine discovers handlers by naming convention, so it would mistake CreateGadgetHandler for a
            // message handler of CreateGadget. The exchange is not named here - Building Blocks owns it.
            options.SubscribeToIntegrationEvents(
                "eventsourced.integration-events",
                typeof(SampleEventSourcedInfrastructure).Assembly,
                "sample.*");
        });

        services.AddDbContext<GadgetReadDbContext>(builder => builder.UseNpgsql(readConnectionString));

        services.AddScoped<IGadgetReadStore, GadgetReadStore>();

        return builder;
    }
}
