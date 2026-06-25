using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.EventProcessing;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence;

/// <summary>
/// A reusable EF Core <see cref="DbContext"/> that acts as an
/// <see cref="IUnitOfWork"/> and collects domain events from tracked
/// aggregate roots, dispatching them after a successful save.
/// </summary>
public abstract class ApplicationDbContextBase : DbContext, IUnitOfWork, IDomainEventsAccessor
{
    private readonly IDomainEventDispatcher? _dispatcher;

    /// <summary>Initializes the context with options and an optional dispatcher.</summary>
    /// <param name="options">The context options.</param>
    /// <param name="dispatcher">An optional dispatcher used to publish collected events.</param>
    protected ApplicationDbContextBase(
        DbContextOptions options,
        IDomainEventDispatcher? dispatcher = null)
        : base(options)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
        => GetEventSources()
            .SelectMany(source => source.DomainEvents)
            .ToList();

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        foreach (var source in GetEventSources())
        {
            source.ClearDomainEvents();
        }
    }

    /// <summary>
    /// Saves all changes and then dispatches any domain events recorded by
    /// tracked aggregates. Events are cleared before dispatch to avoid re-entry.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = GetDomainEvents();
        ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (_dispatcher is not null && domainEvents.Count > 0)
        {
            await _dispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        return result;
    }

    private IReadOnlyList<IDomainEventSource> GetEventSources()
        => ChangeTracker
            .Entries<IDomainEventSource>()
            .Select(entry => entry.Entity)
            .ToList();
}