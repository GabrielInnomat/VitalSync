using System.Reflection;
using BuildingBlocks.Application;
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
/// A host registers its handlers via <see cref="AddCommandsAndQueriesFrom"/>, exactly one persistence style per write
/// database (<see cref="UseEfCorePersistence{TContext}"/> for state-stored contexts, ADR-0020, or
/// <see cref="UseMartenEventSourcing"/> for event-sourced contexts, ADR-0019), and optionally the Wolverine transport
/// for integration events via <see cref="UseWolverineMessaging"/> (ADR-0023). The methods only register services —
/// nothing is built or connected until the host starts.
/// </remarks>
public sealed class BuildingBlocksOptions
{
    private static readonly Type[] HandlerInterfaceDefinitions =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IProjectionHandler<>),
    ];

    private readonly IServiceCollection _services;

    internal BuildingBlocksOptions(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Scans an assembly and registers every command, query, projection handler, and integration-event mapper it contains.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handler implementations.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is <see langword="null"/>.</exception>
    public BuildingBlocksOptions AddCommandsAndQueriesFrom(Assembly assembly)
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
    /// Enables EF Core persistence against the context's write database (state-stored contexts, ADR-0020).
    /// </summary>
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
    public BuildingBlocksOptions UseEfCorePersistence<TContext>()
        where TContext : DbContext
    {
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
    /// setup for the outbox to actually be dispatched.
    /// </remarks>
    /// <param name="connectionString">The connection string of the context's write database.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is <see langword="null"/>.</exception>
    public BuildingBlocksOptions UseMartenEventSourcing(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        _services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.Events.StreamIdentity = StreamIdentity.AsString;
        }).UseLightweightSessions()
            .IntegrateWithWolverine();

        _services.TryAddScoped<MartenAggregateTracker>();
        _services.TryAddScoped<IUnitOfWork, MartenUnitOfWork>();
        _services.TryAddScoped(typeof(IEventSourcedRepository<,>), typeof(MartenEventSourcedRepository<,>));
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
