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
    /// <summary>
    /// Registers everything the sample service needs; the host adds nothing on top.
    /// </summary>
    public static IHostApplicationBuilder AddSampleStateStoredInfrastructure(
        this IHostApplicationBuilder builder,
        string writeConnectionString,
        string readConnectionString,
        Uri rabbitMqUri)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        // The write context is registered by Building Blocks, never by the host (ADR-0027): only then is
        // the outbox guaranteed to enlist in the same transaction as SaveChanges. Taking the builder rather
        // than the collection is what lets Building Blocks own UseWolverine too - the write connection string
        // is named once, here, and the EF outbox reads it back from this selection.
        builder.AddBuildingBlocks(options =>
        {
            // Two assemblies: command and query handlers live in Application, projection handlers and the
            // integration-event mapper in Infrastructure. The scan covers IProjectionHandler<> and
            // IIntegrationEventMapper as well, so neither needs registering by hand - which matters because a
            // forgotten projection fails silently: the read model simply never updates.
            options.AddHandlersFrom(typeof(CreateWidget).Assembly);
            options.AddHandlersFrom(typeof(SampleStateStoredInfrastructure).Assembly);
            options.UseEfCorePersistence<WidgetWriteDbContext>(writeConnectionString);
            options.UseWolverineMessaging(rabbitMqUri);
        });

        // The read context is the service's own business - it carries no outbox and no aggregates, so a
        // plain registration is correct here.
        services.AddDbContext<WidgetReadDbContext>(builder => builder.UseNpgsql(readConnectionString));

        services.AddScoped<IWidgetReadStore, WidgetReadStore>();

        return builder;
    }
}
