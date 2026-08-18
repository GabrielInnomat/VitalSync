using GaWeCodes.Application.Persistence;
using GaWeCodes.DependencyInjection.Wiring;
using GaWeCodes.Diagnostics;
using GaWeCodes.Messaging.DomainEvents;
using GaWeCodes.ReadModels;
using GaWeCodes.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine.EntityFrameworkCore;

namespace GaWeCodes.Persistence.StateStored;

public sealed record EfCorePersistenceAdapter<TContext> : IPersistenceAdapter
    where TContext : DbContext
{
    private readonly IEfCoreDatabaseDriver _driver;
    private readonly Action<DbContextOptionsBuilder>? _configureContext;

    public EfCorePersistenceAdapter(
        IEfCoreDatabaseDriver driver,
        string writeConnectionString,
        Action<DbContextOptionsBuilder>? configureContext = null)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(writeConnectionString);

        _driver = driver;
        _configureContext = configureContext;
        WriteConnectionString = writeConnectionString;
    }

    public string Description => "UseEfCorePersistence";

    public string WriteConnectionString { get; }

    public AggregateStyle AggregateStyle => AggregateStyle.StateStored;

    public bool IsTransientFault(Exception exception) => _driver.IsTransientFault(exception);

    public void Register(PersistenceRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.Services;
        var connectionString = WriteConnectionString;
        var driver = _driver;
        var configureContext = _configureContext;

        services.AddDbContextWithWolverineIntegration<TContext>(builder =>
        {
            driver.ConfigureContext(builder, connectionString);
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

        foreach (var translator in driver.FaultTranslators)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton(translator));
        }

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IStartupCheck, AggregateStateModelCheck<TContext>>());
        services.AddSingleton<IStartupCheck>(new WriteDbContextLifetimeCheck<TContext>(services));
        context.UseWolverineRuntime()
            .AddOutboxDurability(new EfCoreOutboxDurability(driver, connectionString));
        DeadLetterHealthCheckRegistration.Register(services);
    }
}
