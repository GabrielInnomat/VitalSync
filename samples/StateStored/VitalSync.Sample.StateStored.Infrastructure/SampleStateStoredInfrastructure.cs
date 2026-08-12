using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.StateStored.Application;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure.Read;
using VitalSync.Sample.StateStored.Infrastructure.Write;

namespace VitalSync.Sample.StateStored.Infrastructure;

public static class SampleStateStoredInfrastructure
{
    public const string ContextName = SampleContexts.StateStored;

    public static IHostApplicationBuilder AddSampleStateStoredInfrastructure(
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
            options.AddHandlersFrom(typeof(CreateWidget).Assembly);
            options.AddHandlersFrom(typeof(SampleStateStoredInfrastructure).Assembly);
            options.AddDomainEventsFrom(typeof(Widget).Assembly);
            options.UseEfCorePersistence<WidgetWriteDbContext>(writeConnectionString);
            options.UseWolverineMessaging(rabbitMqUri, exchangeName, ContextName);
            options.ProvisionInfrastructure(provisioning);
        });

        services.AddDbContext<WidgetReadDbContext>(builder => builder.UseNpgsql(readConnectionString));

        services.AddScoped<IWidgetReadStore, WidgetReadStore>();

        return builder;
    }
}
