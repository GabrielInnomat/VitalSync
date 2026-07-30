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
/// throws — a context that appears to need both is cut wrong and should be split. The methods only register services —
/// nothing is built or connected until the host starts.
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

    private static readonly Type[] HandlerInterfaceDefinitions =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IProjectionHandler<>),
    ];

    private readonly IServiceCollection _services;
    private readonly PipelineBehaviorRegistry _behaviorRegistry;
    private PersistenceStyle _persistenceStyle;

    internal BuildingBlocksOptions(IServiceCollection services, PipelineBehaviorRegistry behaviorRegistry)
    {
        _services = services;
        _behaviorRegistry = behaviorRegistry;
    }

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
    /// <param name="assembly">The assembly to scan for handler implementations.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is <see langword="null"/>.</exception>
    public BuildingBlocksOptions AddHandlersFrom(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false } || type.IsGenericTypeDefinition)
            {
                continue;
            }

            foreach (var contract in type.GetInterfaces())
            {
                if (contract == typeof(IIntegrationEventMapper))
                {
                    _services.AddScoped(typeof(IIntegrationEventMapper), type);
                }
                else if (contract.IsGenericType
                    && Array.IndexOf(HandlerInterfaceDefinitions, contract.GetGenericTypeDefinition()) >= 0)
                {
                    _services.AddScoped(contract, type);
                }
            }
        }

        return this;
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
    /// <remarks>
    /// The host must register <typeparamref name="TContext"/> itself, via
    /// <c>AddDbContextWithWolverineIntegration&lt;TContext&gt;</c> (not plain <c>AddDbContext</c>) against the
    /// write-database connection string of the ADR-0021 pair, and must apply
    /// <see cref="WolverineOptionsExtensions.ApplyBuildingBlockEfCoreOutbox"/> from its <c>UseWolverine</c> setup —
    /// both are required for <see cref="IDbContextOutbox{TContext}"/> to enlist outgoing messages in the same
    /// transaction as <typeparamref name="TContext"/>'s <c>SaveChanges</c> (ADR-0022/0023). This method wires the unit
    /// of work and the generic repository on top of that context.
    /// </remarks>
    /// <typeparam name="TContext">The write-database context type of the bounded context.</typeparam>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="UseMartenEventSourcing"/> was already selected for this host. A bounded context uses exactly one persistence strategy (ADR-0019/0020/0021); mixing EF Core and Marten is not supported.</exception>
    public BuildingBlocksOptions UseEfCorePersistence<TContext>()
        where TContext : DbContext
    {
        SelectPersistenceStyle(PersistenceStyle.EfCore);

        _services.TryAddScoped<DbContext>(static provider => provider.GetRequiredService<TContext>());
        _services.TryAddScoped<IUnitOfWork, EfCoreUnitOfWork<TContext>>();
        _services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));
        return this;
    }

    /// <summary>
    /// Enables Marten event sourcing against the context's write database (event-sourced contexts, ADR-0019).
    /// </summary>
    /// <remarks>
    /// Registers Marten with string stream identities and lightweight sessions on the given write-database connection
    /// string of the ADR-0021 pair, integrates it with Wolverine so a session can be enrolled in the messaging
    /// transport's transactional outbox (ADR-0023), and wires the unit of work and the event-sourced repository on top
    /// of it. The host must still apply
    /// <see cref="WolverineOptionsExtensions.ApplyBuildingBlockDomainEventRouting"/> from its <c>UseWolverine</c>
    /// setup for the outbox to actually be dispatched. Both persistence styles register the same
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
        return this;
    }

    /// <summary>
    /// Enables the Wolverine/RabbitMQ transport for integration events (ADR-0023).
    /// </summary>
    /// <remarks>
    /// Registers the Wolverine-backed transport so the publisher hands mapped integration events to the broker. The
    /// host must additionally run Wolverine (e.g. <c>UseWolverine</c>) and is expected to apply
    /// <see cref="WolverineOptionsExtensions.ApplyBuildingBlockMessagingDefaults"/> for the RabbitMQ connection,
    /// retries, and dead-lettering.
    /// </remarks>
    /// <returns>The same options, for chaining.</returns>
    public BuildingBlocksOptions UseWolverineMessaging()
    {
        _services.Replace(ServiceDescriptor.Scoped<IIntegrationEventTransport, WolverineIntegrationEventTransport>());
        return this;
    }
}
