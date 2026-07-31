using System.Reflection;
using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using JasperFx.Events;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine.EntityFrameworkCore;
using Wolverine.Marten;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// Builder used by <see cref="ServiceCollectionExtensions.AddBuildingBlocks"/> to select a host's Building Block capabilities.
/// </summary>
/// <remarks>
/// A host registers its handlers via <see cref="AddHandlersFrom"/>, exactly one persistence style per write
/// database (<see cref="UseEfCorePersistence{TContext}"/> for state-stored contexts, ADR-0020, or
/// <see cref="UseMartenEventSourcing"/> for event-sourced contexts, ADR-0019), and optionally the Wolverine transport
/// for integration events via <see cref="UseWolverineMessaging"/> (ADR-0023). A microservice hosts exactly one
/// bounded context and a bounded context uses exactly one persistence strategy, so selecting both persistence styles
/// throws — a context that appears to need both is cut wrong and should be split. Each selection also records which
/// Wolverine defaults it needs; a registered <see cref="BuildingBlocksWolverineExtension"/> applies them when the
/// host calls <c>UseWolverine</c>, so the host performs no Wolverine configuration of its own (ADR-0027). The
/// methods only register services — nothing is built or connected until the host starts.
/// </remarks>
public sealed class BuildingBlocksOptions
{
    /// <summary>
    /// The execution order of the built-in logging behavior — the outermost built-in behavior.
    /// </summary>
    /// <remarks>
    /// Logging wraps every other built-in so that failures translated further in (expected domain errors, concurrency
    /// conflicts) are observed as failed results and logged at <c>Warning</c>, while only genuinely unexpected
    /// exceptions surface as <c>Error</c>. Register a custom behavior with a smaller order to run outside logging.
    /// </remarks>
    public const int LoggingBehaviorOrder = 0;

    /// <summary>
    /// The execution order of the built-in exception-to-result translation behavior.
    /// </summary>
    /// <remarks>
    /// Sits inside logging (<see cref="LoggingBehaviorOrder"/>) and outside the unit of work
    /// (<see cref="UnitOfWorkBehaviorOrder"/>) so expected domain exceptions become failed results before logging sees
    /// them and before any transaction is committed.
    /// </remarks>
    public const int ExceptionToResultBehaviorOrder = 100;

    /// <summary>
    /// The execution order of the built-in unit-of-work behavior — the innermost built-in behavior.
    /// </summary>
    /// <remarks>
    /// Runs closest to the handler so exactly one unit of work spans the handler and commits only on success. The gap
    /// to <see cref="ExceptionToResultBehaviorOrder"/> leaves room (for example order <c>200</c>) for a future
    /// input-validation behavior that should run outside the transaction.
    /// </remarks>
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
    private PersistenceStyle _persistenceStyle;

    internal BuildingBlocksOptions(IServiceCollection services, PipelineBehaviorRegistry behaviorRegistry)
    {
        _services = services;
        _behaviorRegistry = behaviorRegistry;
    }

    /// <summary>
    /// Gets or sets a value indicating whether handler registration is verified when the host starts.
    /// </summary>
    /// <remarks>
    /// Enabled by default: a startup check resolves the handler for every <see cref="ICommand"/>,
    /// <see cref="ICommand{TResult}"/>, and <see cref="IQuery{TResult}"/> implementation found in the assemblies given
    /// to <see cref="AddHandlersFrom"/>, so a missing or unregistered handler fails the host at startup with the
    /// offending request types named — instead of surfacing as "no service registered" on the first request in
    /// production. Set to <see langword="false"/> only when a host intentionally registers handlers outside the
    /// assembly scan and the check would report false positives. The check runs as a hosted service, so it never
    /// affects code that builds a bare service provider without starting a host (for example unit tests).
    /// </remarks>
    /// <value><c>true</c> if handler registration is verified at host startup; otherwise, <c>false</c>. The default is <c>true</c>.</value>
    public bool ValidateHandlersOnStart { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the Wolverine wiring is verified when the host starts.
    /// </summary>
    /// <remarks>
    /// Enabled by default: when the host selects a capability that flows through Wolverine's transactional outbox
    /// (a persistence style or <see cref="UseWolverineMessaging"/>), a startup check verifies that the host actually
    /// called <c>UseWolverine</c> — the one wiring step that cannot be performed from a service collection — and
    /// fails the host at startup with an actionable message instead of surfacing as a missing outbox on the first
    /// commit in production (ADR-0027). Set to <see langword="false"/> only for hosts that intentionally run those
    /// code paths without Wolverine (for example certain test hosts). The check runs as a hosted service, so it
    /// never affects code that builds a bare service provider without starting a host.
    /// </remarks>
    /// <value><c>true</c> if the Wolverine wiring is verified at host startup; otherwise, <c>false</c>. The default is <c>true</c>.</value>
    public bool ValidateWolverineOnStart { get; set; } = true;

    internal WolverineWiringSettings WolverineWiring { get; } = new();

    internal IReadOnlyCollection<Assembly> ScannedAssemblies => _scannedAssemblies;

    private enum PersistenceStyle
    {
        None = 0,
        EfCore,
        Marten,
    }

    private void SelectPersistenceStyle(PersistenceStyle style)
    {
        if (_persistenceStyle != PersistenceStyle.None && _persistenceStyle != style)
        {
            throw new InvalidOperationException(
                "Two persistence strategies were configured for the same host (EF Core and Marten). " +
                "A microservice hosts exactly one bounded context, and a bounded context uses exactly one " +
                "persistence strategy (ADR-0019/0020/0021): state-stored via EF Core, or event-sourced via Marten. " +
                "A commit cannot span both stores atomically because they live in separate databases (ADR-0020). " +
                "A context that appears to need both is a sign it is cut wrong and should be split into two " +
                "bounded contexts, each in its own microservice with its own single persistence strategy.");
        }

        _persistenceStyle = style;
    }

    /// <summary>
    /// Scans an assembly and registers every command, query, projection handler, and integration-event mapper it contains.
    /// </summary>
    /// <remarks>
    /// Registration is idempotent for the multi-handler contracts (<see cref="IProjectionHandler{TEvent}"/> and
    /// <see cref="IIntegrationEventMapper"/>): scanning the same assembly twice does not register a handler twice, so a
    /// projection never runs more than once per event, while two <em>different</em> handlers for the same event both
    /// remain registered. The single-handler contracts (<see cref="ICommandHandler{TCommand}"/>,
    /// <see cref="ICommandHandler{TCommand, TResult}"/>, <see cref="IQueryHandler{TQuery, TResult}"/>) must resolve to
    /// exactly one implementation; discovering two <em>different</em> handlers for the same command or query is a
    /// modelling error and throws immediately, rather than letting the container silently pick one at request time.
    /// Scanned assemblies are additionally recorded for the startup handler check
    /// (<see cref="ValidateHandlersOnStart"/>), which verifies at host start that every command and query found in
    /// them resolves to a handler.
    /// </remarks>
    /// <param name="assembly">The assembly to scan for handler implementations.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assembly's types cannot be loaded (most often a missing package reference), or when two different handlers are found for the same command or query contract.</exception>
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

    /// <summary>
    /// Registers a custom open-generic pipeline behavior at an explicit position in the dispatch pipeline.
    /// </summary>
    /// <remarks>
    /// The pipeline wraps behaviors by ascending <paramref name="order"/>: a lower order runs further out (earlier),
    /// a higher order sits closer to the handler. The built-in behaviors occupy fixed slots
    /// (<see cref="LoggingBehaviorOrder"/>, <see cref="ExceptionToResultBehaviorOrder"/>,
    /// <see cref="UnitOfWorkBehaviorOrder"/>); by convention give a behavior a negative order to run before all
    /// built-ins, or an order above <see cref="UnitOfWorkBehaviorOrder"/> to run after them. Use this to place
    /// cross-cutting concerns such as authorization, multi-tenancy, idempotency, or caching deterministically instead
    /// of relying on registration order. <paramref name="openGenericBehavior"/> must be an open-generic type definition
    /// with two type parameters that implements <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
    /// </remarks>
    /// <param name="openGenericBehavior">The open-generic behavior type definition, for example <c>typeof(MyBehavior&lt;,&gt;)</c>.</param>
    /// <param name="order">The execution order; lower values wrap further out and execute earlier.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openGenericBehavior"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="openGenericBehavior"/> is not an open-generic type definition, does not have exactly two type parameters, or does not implement <see cref="IPipelineBehavior{TRequest, TResponse}"/>.</exception>
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
    /// <summary>
    /// Enables EF Core persistence against the context's write database (state-stored contexts, ADR-0020).
    /// </summary>
    /// <remarks>
    /// Registers <typeparamref name="TContext"/> itself, via Wolverine's
    /// <c>AddDbContextWithWolverineIntegration&lt;TContext&gt;</c> on the Npgsql provider (PostgreSQL is the single
    /// relational engine, ADR-0020), so outgoing messages enlist in the same transaction as the context's
    /// <c>SaveChanges</c> (ADR-0022/0023) — the host never registers the context and therefore cannot break the
    /// single-transaction guarantee with a plain <c>AddDbContext</c> (ADR-0027). Wolverine's EF Core transactional
    /// middleware and its PostgreSQL-backed durable message store — which the EF Core outbox requires — are applied
    /// automatically on the same write database when the host calls <c>UseWolverine</c>. On top of that context this
    /// method wires the unit of work and the generic repository. Use <paramref name="configureContext"/> for
    /// additional provider options; Aspire hosts enrich the registration afterwards (for example
    /// <c>EnrichNpgsqlDbContext</c>) rather than re-registering it.
    /// </remarks>
    /// <typeparam name="TContext">The write-database context type of the bounded context.</typeparam>
    /// <param name="connectionString">The connection string of the context's write database (the write half of the ADR-0021 pair).</param>
    /// <param name="configureContext">An optional callback for additional context configuration beyond the Npgsql provider setup.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="UseMartenEventSourcing"/> was already selected for this host. A bounded context uses exactly one persistence strategy (ADR-0019/0020/0021); mixing EF Core and Marten is not supported.</exception>
    public BuildingBlocksOptions UseEfCorePersistence<TContext>(
        string connectionString,
        Action<DbContextOptionsBuilder>? configureContext = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        SelectPersistenceStyle(PersistenceStyle.EfCore);

        _services.AddDbContextWithWolverineIntegration<TContext>(builder =>
        {
            builder.UseNpgsql(connectionString);
            configureContext?.Invoke(builder);
        });

        _services.TryAddScoped<DbContext>(static provider => provider.GetRequiredService<TContext>());
        _services.TryAddScoped<IUnitOfWork, EfCoreUnitOfWork<TContext>>();
        _services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));

        // The durable message store must be registered here, at composition time — container-registered
        // Wolverine extensions run after the provider is built, where service registrations no longer take
        // effect (see EfCoreMessageStoreRegistration).
        EfCoreMessageStoreRegistration.Register(_services, connectionString);

        WolverineWiring.ApplyDomainEventRouting = true;
        WolverineWiring.EfCoreMessageStoreConnectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Enables Marten event sourcing against the context's write database (event-sourced contexts, ADR-0019).
    /// </summary>
    /// <remarks>
    /// Registers Marten with string stream identities and lightweight sessions on the given write-database connection
    /// string of the ADR-0021 pair, integrates it with Wolverine so a session can be enrolled in the messaging
    /// transport's transactional outbox (ADR-0023), and wires the unit of work and the event-sourced repository on top
    /// of it. The domain-event routing is applied automatically when the host calls <c>UseWolverine</c>
    /// (ADR-0027). Both persistence styles register the same
    /// <see cref="IRepository{TAggregate, TKey}"/> contract; select exactly one style per write database.
    /// </remarks>
    /// <param name="connectionString">The connection string of the context's write database.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="UseEfCorePersistence{TContext}"/> was already selected for this host. A bounded context uses exactly one persistence strategy (ADR-0019/0020/0021); mixing EF Core and Marten is not supported.</exception>
    public BuildingBlocksOptions UseMartenEventSourcing(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        SelectPersistenceStyle(PersistenceStyle.Marten);

        _services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.Events.StreamIdentity = StreamIdentity.AsString;
        }).UseLightweightSessions()
            .IntegrateWithWolverine();

        _services.TryAddScoped<MartenAggregateTracker>();
        _services.TryAddScoped<IUnitOfWork, MartenUnitOfWork>();
        _services.TryAddScoped(typeof(IRepository<,>), typeof(MartenEventSourcedRepository<,>));

        WolverineWiring.ApplyDomainEventRouting = true;
        return this;
    }

    /// <summary>
    /// Enables the Wolverine/RabbitMQ transport for integration events (ADR-0023).
    /// </summary>
    /// <remarks>
    /// Registers the Wolverine-backed sink factory so the envelope handler binds mapped integration events to the
    /// handled message's context (enrolled in its outbox, correlation propagated), and
    /// records the broker URI so the RabbitMQ transport, retry, and dead-letter defaults are applied automatically
    /// when the host calls <c>UseWolverine</c> (ADR-0027) — the host passes the Aspire-provided connection string
    /// here and configures nothing else.
    /// </remarks>
    /// <param name="rabbitMqUri">The AMQP connection URI of the RabbitMQ broker (typically the Aspire-provided connection string).</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rabbitMqUri"/> is <see langword="null"/>.</exception>
    public BuildingBlocksOptions UseWolverineMessaging(Uri rabbitMqUri)
    {
        ArgumentNullException.ThrowIfNull(rabbitMqUri);

        _services.Replace(ServiceDescriptor.Singleton<IIntegrationEventSinkFactory, WolverineIntegrationEventSinkFactory>());
        WolverineWiring.RabbitMqUri = rabbitMqUri;
        return this;
    }
}
