using BuildingBlocks.Application.ReadModels;
using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Domain.Entities;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.ReadModels;

public sealed class ReadModelRebuildRunner<TContext>(IServiceScopeFactory scopeFactory)
    where TContext : DbContext
{
    private const int BatchSize = 500;

    public async Task RebuildAsync<TAggregate, TKey, TState>(CancellationToken cancellationToken)
        where TAggregate : class, IAggregateRoot<TKey>
        where TKey : struct, IEntityKey, IEquatable<TKey>
        where TState : class
    {
        await ClearAsync<TAggregate, TKey>(cancellationToken).ConfigureAwait(false);

        using var readScope = scopeFactory.CreateScope();
        var context = readScope.ServiceProvider.GetRequiredService<TContext>();

        var batch = new List<TAggregate>(BatchSize);

        await foreach (var state in context.Set<TState>()
            .AsNoTracking()
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            batch.Add(Rehydrate<TAggregate>(state));

            if (batch.Count < BatchSize)
            {
                continue;
            }

            await RebuildBatchAsync<TAggregate, TKey>(batch, cancellationToken).ConfigureAwait(false);
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            await RebuildBatchAsync<TAggregate, TKey>(batch, cancellationToken).ConfigureAwait(false);
        }
    }

    private static TAggregate Rehydrate<TAggregate>(object state)
        where TAggregate : class
    {
        var aggregate = AggregateFactory.CreateEmpty<TAggregate>();

        if (aggregate is not IStateOwner stateOwner)
        {
            throw new InvalidOperationException(
                $"The aggregate '{typeof(TAggregate)}' does not expose its state and cannot be rebuilt from the write database.");
        }

        stateOwner.Restore(state);
        return aggregate;
    }

    private static IReadModelRebuilder<TAggregate, TKey>[] RebuildersOf<TAggregate, TKey>(
        IServiceProvider services)
        where TAggregate : class, IAggregateRoot<TKey>
        where TKey : struct, IEntityKey, IEquatable<TKey>
    {
        var rebuilders = services.GetServices<IReadModelRebuilder<TAggregate, TKey>>().ToArray();

        return rebuilders.Length > 0
            ? rebuilders
            : throw new InvalidOperationException(
                $"No {typeof(IReadModelRebuilder<,>).Name} was registered for aggregate '{typeof(TAggregate)}'. " +
                "A rebuild that projects nothing reports success while the read model stays empty; " +
                "register one through AddHandlersFrom, or do not run the rebuild.");
    }

    private async Task ClearAsync<TAggregate, TKey>(CancellationToken cancellationToken)
        where TAggregate : class, IAggregateRoot<TKey>
        where TKey : struct, IEntityKey, IEquatable<TKey>
    {
        using var scope = scopeFactory.CreateScope();

        foreach (var rebuilder in RebuildersOf<TAggregate, TKey>(scope.ServiceProvider))
        {
            await rebuilder.ClearAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RebuildBatchAsync<TAggregate, TKey>(
        IReadOnlyList<TAggregate> batch,
        CancellationToken cancellationToken)
        where TAggregate : class, IAggregateRoot<TKey>
        where TKey : struct, IEntityKey, IEquatable<TKey>
    {
        using var scope = scopeFactory.CreateScope();
        var rebuilders = RebuildersOf<TAggregate, TKey>(scope.ServiceProvider);

        foreach (var aggregate in batch)
        {
            foreach (var rebuilder in rebuilders)
            {
                await rebuilder.RebuildAsync(aggregate, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
