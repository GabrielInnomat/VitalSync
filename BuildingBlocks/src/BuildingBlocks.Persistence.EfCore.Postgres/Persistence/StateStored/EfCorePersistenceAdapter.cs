using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Diagnostics;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.ReadModels;
using BuildingBlocks.Infrastructure.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.StateStored;

internal sealed record EfCorePersistenceAdapter<TContext>(
    string WriteConnectionString,
    Action<DbContextOptionsBuilder>? ConfigureContext) : IPersistenceAdapter
    where TContext : DbContext
{
    public string Description => "UseEfCorePersistence";

    public bool IsTransientFault(Exception exception) => PostgresTransientFaults.IsTransient(exception);

    public void Register(PersistenceRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.Services;
        var connectionString = WriteConnectionString;
        var configureContext = ConfigureContext;

        services.AddDbContextWithWolverineIntegration<TContext>(builder =>
        {
            builder.UseNpgsql(connectionString);
            configureContext?.Invoke(builder);
        });

        services.TryAddScoped(static provider =>
            new WriteDbContextAccessor(provider.GetRequiredService<TContext>()));
        services.TryAddScoped<EfCoreAggregateTracker>();
        services.TryAddSingleton<DomainEventEnvelopeFactory>();
        services.TryAddSingleton<StateStoredReadModelRebuildRunner<TContext>>();
        services.TryAddScoped<IUnitOfWork, EfCoreUnitOfWork<TContext>>();
        services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersistenceFaultTranslator, EfCoreFaultTranslator>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersistenceFaultTranslator, PostgresFaultTranslator>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IStartupCheck, AggregateStateModelCheck<TContext>>());
        context.UseWolverineRuntime()
            .AddOutboxDurability(new EfCoreOutboxDurability(connectionString));
        DeadLetterHealthCheckRegistration.Register(services);
    }
}
