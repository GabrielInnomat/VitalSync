using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
using JasperFx.Events;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine.EntityFrameworkCore;
using Wolverine.Marten;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Registration;

internal sealed class PersistenceRegistrar(IServiceCollection services, WolverineWiringSettings wiring)
{
    public void UseEfCore<TContext>(string connectionString, Action<DbContextOptionsBuilder>? configureContext)
        where TContext : DbContext
    {
        wiring.SelectPersistence(PersistenceChoice.EfCore(connectionString));

        services.AddDbContextWithWolverineIntegration<TContext>(builder =>
        {
            builder.UseNpgsql(connectionString);
            configureContext?.Invoke(builder);
        });

        services.TryAddScoped<DbContext>(static provider => provider.GetRequiredService<TContext>());
        services.TryAddScoped<EfCoreAggregateTracker>();
        services.TryAddSingleton<DomainEventEnvelopeFactory>();
        services.TryAddScoped<IUnitOfWork, EfCoreUnitOfWork<TContext>>();
        services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IStartupCheck, AggregateStateModelCheck<TContext>>());
    }

    public void UseMarten(string connectionString)
    {
        wiring.SelectPersistence(PersistenceChoice.Marten);

        services.AddMarten(serviceProvider =>
        {
            var storeOptions = new StoreOptions();
            storeOptions.Connection(connectionString);
            storeOptions.Events.StreamIdentity = StreamIdentity.AsString;

            foreach (var (domainEventType, eventName) in serviceProvider
                .GetRequiredService<DomainEventTypeRegistry>()
                .NamesByType)
            {
                storeOptions.Events.MapEventType(domainEventType, eventName);
            }

            return storeOptions;
        }).UseLightweightSessions()
            .IntegrateWithWolverine();

        services.TryAddScoped<MartenAggregateTracker>();
        services.TryAddSingleton<DomainEventEnvelopeFactory>();
        services.TryAddScoped<IUnitOfWork, MartenUnitOfWork>();
        services.TryAddScoped(typeof(IRepository<,>), typeof(MartenEventSourcedRepository<,>));
    }
}
