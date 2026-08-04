using System.Reflection;
using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
using BuildingBlocks.Infrastructure.Persistence;
using JasperFx.Events;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.EntityFrameworkCore;
using Wolverine.Marten;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

public sealed class BuildingBlocksOptions
{
    public const int LoggingBehaviorOrder = 0;

    public const int ExceptionToResultBehaviorOrder = 100;

    public const int UnitOfWorkBehaviorOrder = 300;

    private static readonly Type[] SingleHandlerInterfaceDefinitions =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    private static readonly Type[] MultiHandlerInterfaceDefinitions =
    [
        typeof(IProjectionHandler<>),
    ];

    private readonly IServiceCollection _services;
    private readonly PipelineBehaviorRegistry _behaviorRegistry;
    private readonly Dictionary<Type, Type> _singleHandlers = [];
    private readonly HashSet<Assembly> _scannedAssemblies = [];
    private readonly HashSet<Assembly> _domainEventAssemblies = [];
    private DomainEventTypeRegistry? _domainEventTypeRegistry;

    internal BuildingBlocksOptions(IServiceCollection services, PipelineBehaviorRegistry behaviorRegistry)
    {
        _services = services;
        _behaviorRegistry = behaviorRegistry;
    }

    internal WolverineWiringSettings WolverineWiring { get; } = new();

    internal IReadOnlyCollection<Assembly> ScannedAssemblies => _scannedAssemblies;

    internal IReadOnlyCollection<Assembly> DomainEventAssemblies => _domainEventAssemblies;

    internal DomainEventTypeRegistry DomainEventTypeRegistry =>
        _domainEventTypeRegistry ??= new DomainEventTypeRegistry(_domainEventAssemblies);

    public BuildingBlocksOptions AddHandlersFrom(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            throw new InvalidOperationException(
                $"The types of assembly '{assembly.FullName}' could not be loaded. " +
                "The most common cause is a missing package reference.",
                exception);
        }

        _scannedAssemblies.Add(assembly);

        foreach (var type in types)
        {
            if (type is not { IsClass: true, IsAbstract: false } || type.IsGenericTypeDefinition)
            {
                continue;
            }

            foreach (var contract in type.GetInterfaces())
            {
                if (contract == typeof(IIntegrationEventMapper))
                {
                    _services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IIntegrationEventMapper), type));
                }
                else if (contract.IsGenericType
                    && Array.IndexOf(MultiHandlerInterfaceDefinitions, contract.GetGenericTypeDefinition()) >= 0)
                {
                    _services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, type));
                }
                else if (contract.IsGenericType
                    && Array.IndexOf(SingleHandlerInterfaceDefinitions, contract.GetGenericTypeDefinition()) >= 0)
                {
                    RegisterSingleHandler(contract, type);
                }
            }
        }

        return this;
    }

    private void RegisterSingleHandler(Type contract, Type implementation)
    {
        if (_singleHandlers.TryGetValue(contract, out var existing))
        {
            if (existing == implementation)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Two handlers were found for '{contract}': '{existing}' and '{implementation}'. " +
                "A command or query must have exactly one handler.");
        }

        _singleHandlers.Add(contract, implementation);
        _services.AddScoped(contract, implementation);
    }

    public BuildingBlocksOptions AddDomainEventsFrom(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (_domainEventTypeRegistry is not null)
        {
            throw new InvalidOperationException(
                "AddDomainEventsFrom was called after the domain event names had already been read. " +
                "Register every domain event assembly inside the AddBuildingBlocks callback.");
        }

        _domainEventAssemblies.Add(assembly);
        return this;
    }

    public BuildingBlocksOptions AddPipelineBehavior(Type openGenericBehavior, int order)
    {
        ArgumentNullException.ThrowIfNull(openGenericBehavior);

        if (!openGenericBehavior.IsGenericTypeDefinition || openGenericBehavior.GetGenericArguments().Length != 2)
        {
            throw new ArgumentException(
                "A pipeline behavior must be an open-generic type definition with two type parameters " +
                "(TRequest, TResponse), for example typeof(MyBehavior<,>).",
                nameof(openGenericBehavior));
        }

        var implementsBehavior = Array.Exists(
            openGenericBehavior.GetInterfaces(),
            static @interface => @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!implementsBehavior)
        {
            throw new ArgumentException(
                $"Type '{openGenericBehavior}' does not implement {typeof(IPipelineBehavior<,>)}.",
                nameof(openGenericBehavior));
        }

        _behaviorRegistry.Register(openGenericBehavior, order);
        _services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), openGenericBehavior));
        return this;
    }
    public BuildingBlocksOptions UseEfCorePersistence<TContext>(
        string connectionString,
        Action<DbContextOptionsBuilder>? configureContext = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        WolverineWiring.SelectPersistence(PersistenceChoice.EfCore(connectionString));

        _services.AddDbContextWithWolverineIntegration<TContext>(builder =>
        {
            builder.UseNpgsql(connectionString);
            configureContext?.Invoke(builder);
        });

        _services.TryAddScoped<DbContext>(static provider => provider.GetRequiredService<TContext>());
        _services.TryAddScoped<EfCoreAggregateTracker>();
        _services.TryAddScoped<IUnitOfWork, EfCoreUnitOfWork<TContext>>();
        _services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));
        _services.AddHostedService(static provider => new AggregateStateModelStartupValidator<TContext>(provider));

        return this;
    }

    public BuildingBlocksOptions UseMartenEventSourcing(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        WolverineWiring.SelectPersistence(PersistenceChoice.Marten);

        _services.AddMarten(serviceProvider =>
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

        _services.TryAddScoped<MartenAggregateTracker>();
        _services.TryAddScoped<IUnitOfWork, MartenUnitOfWork>();
        _services.TryAddScoped(typeof(IRepository<,>), typeof(MartenEventSourcedRepository<,>));

        return this;
    }

    public BuildingBlocksOptions UseWolverineMessaging(Uri rabbitMqUri, string exchangeName, string contextName)
    {
        ArgumentNullException.ThrowIfNull(rabbitMqUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        if (!KebabCase.IsValid(contextName))
        {
            throw new ArgumentException(
                $"'{contextName}' is not a valid bounded-context name. It is the first segment of every routing " +
                "key this service publishes, so it must be a single lower-case kebab-case word without a dot " +
                "(for example \"nutrition\"). A value containing a dot is almost always the exchange name passed " +
                "in the wrong position.",
                nameof(contextName));
        }

        _services.Replace(ServiceDescriptor.Singleton<IIntegrationEventSinkFactory>(
            new WolverineIntegrationEventSinkFactory(contextName)));
        _services.Replace(ServiceDescriptor.Singleton(new IntegrationEventSourceContext(contextName)));
        WolverineWiring.SelectMessaging(new MessagingSettings(rabbitMqUri, exchangeName, contextName));
        return this;
    }

    public BuildingBlocksOptions SubscribeToIntegrationEvents(
        string queueName,
        Assembly consumerAssembly,
        params string[] topicPatterns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(consumerAssembly);
        ArgumentNullException.ThrowIfNull(topicPatterns);

        if (topicPatterns.Length == 0 || Array.Exists(topicPatterns, string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "At least one non-blank topic pattern is required. A queue with no binding receives nothing, " +
                "and neither the broker nor Wolverine reports that as an error.",
                nameof(topicPatterns));
        }

        WolverineWiring.SelectSubscription(new IntegrationEventSubscription(
            queueName,
            [.. topicPatterns],
            consumerAssembly));

        return this;
    }
}
