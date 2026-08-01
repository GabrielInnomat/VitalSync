using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Infrastructure.Read;

namespace VitalSync.Sample.EventSourced.Infrastructure;

public static class SampleEventSourcedInfrastructure
{
    /// <summary>
    /// Registers everything the sample service needs; the host adds only <c>UseWolverine()</c> on top.
    /// </summary>
    public static IServiceCollection AddSampleEventSourcedInfrastructure(
        this IServiceCollection services,
        string writeConnectionString,
        string readConnectionString,
        Uri rabbitMqUri)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBuildingBlocks(options =>
        {
            // Two assemblies, same reason as on the state-stored side: handlers in Application, projections
            // and the integration-event mapper in Infrastructure.
            options.AddHandlersFrom(typeof(CreateGadget).Assembly);
            options.AddHandlersFrom(typeof(SampleEventSourcedInfrastructure).Assembly);

            // The one line that differs from the state-stored service. Marten brings its own message store
            // through IntegrateWithWolverine, so unlike the EF Core path the host has nothing left to wire -
            // this is where ADR-0027 is expected to hold without an exception.
            options.UseMartenEventSourcing(writeConnectionString);
            options.UseWolverineMessaging(rabbitMqUri);
        });

        services.AddDbContext<GadgetReadDbContext>(builder => builder.UseNpgsql(readConnectionString));

        services.AddScoped<IGadgetReadStore, GadgetReadStore>();

        return services;
    }
}
