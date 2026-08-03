using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitalSync.Sample.StateStored.Application;
using VitalSync.Sample.StateStored.Domain;
using VitalSync.Sample.StateStored.Infrastructure.Integration;
using VitalSync.Sample.StateStored.Infrastructure.Read;
using VitalSync.Sample.StateStored.Infrastructure.Write;

namespace VitalSync.Sample.StateStored.Infrastructure;

public static class SampleStateStoredInfrastructure
{
    public static IHostApplicationBuilder AddSampleStateStoredInfrastructure(
        this IHostApplicationBuilder builder,
        string writeConnectionString,
        string readConnectionString,
        Uri rabbitMqUri)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        builder.AddBuildingBlocks(options =>
        {
            options.AddHandlersFrom(typeof(CreateWidget).Assembly);
            options.AddHandlersFrom(typeof(SampleStateStoredInfrastructure).Assembly);
            options.UseEfCorePersistence<WidgetWriteDbContext>(writeConnectionString);
            options.UseWolverineMessaging(rabbitMqUri);
        });

        services.AddDbContext<WidgetReadDbContext>(builder => builder.UseNpgsql(readConnectionString));

        services.AddScoped<IWidgetReadStore, WidgetReadStore>();

        return builder;
    }
}
