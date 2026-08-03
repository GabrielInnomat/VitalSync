using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using JasperFx;
using JasperFx.Events;
using Marten;
using NSubstitute;
using Wolverine.Marten;

namespace BuildingBlocks.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class MartenEventSourcedRepositoryTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset CommitTime = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveThenReload_ReturnsTheFoldedStateAndVersion()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        var store = BuildStore();
        var id = new CounterId(Guid.NewGuid());

        await using (var session = store.LightweightSession())
        {
            var tracker = new MartenAggregateTracker();
            var repository = new MartenEventSourcedRepository<Counter, CounterId>(session, tracker);
            var counter = Counter.Create(id);
            counter.Increment(5);
            await repository.AddAsync(counter, TestContext.Current.CancellationToken);

            await Commit(session, tracker);
        }

        await using (var session = store.LightweightSession())
        {
            var tracker = new MartenAggregateTracker();
            var repository = new MartenEventSourcedRepository<Counter, CounterId>(session, tracker);

            var reloaded = await repository.GetByIdAsync(id, TestContext.Current.CancellationToken);

            Assert.NotNull(reloaded);
            Assert.Equal(5, reloaded!.Total);
            Assert.Equal(2, ((IEventSourcedAggregateRoot<CounterId>)reloaded).Version);
        }
    }

    [Fact]
    public async Task Reload_ChangeAndCommit_AdvancesTheVersion()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        var store = BuildStore();
        var id = new CounterId(Guid.NewGuid());

        await Seed(store, id, increments: 5);

        await using (var session = store.LightweightSession())
        {
            var tracker = new MartenAggregateTracker();
            var repository = new MartenEventSourcedRepository<Counter, CounterId>(session, tracker);
            var counter = await repository.GetByIdAsync(id, TestContext.Current.CancellationToken);
            counter!.Increment(3);

            await Commit(session, tracker);
        }

        await using (var verification = store.LightweightSession())
        {
            var repository = new MartenEventSourcedRepository<Counter, CounterId>(verification, new MartenAggregateTracker());
            var reloaded = await repository.GetByIdAsync(id, TestContext.Current.CancellationToken);

            Assert.Equal(8, reloaded!.Total);
            Assert.Equal(3, ((IEventSourcedAggregateRoot<CounterId>)reloaded).Version);
        }
    }

    [Fact]
    public async Task ConcurrentCommitsOnTheSameStream_RaiseAConcurrencyException()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        var store = BuildStore();
        var id = new CounterId(Guid.NewGuid());
        await Seed(store, id, increments: 1);

        await using var sessionA = store.LightweightSession();
        var trackerA = new MartenAggregateTracker();
        var repositoryA = new MartenEventSourcedRepository<Counter, CounterId>(sessionA, trackerA);
        var counterA = await repositoryA.GetByIdAsync(id, TestContext.Current.CancellationToken);

        await using var sessionB = store.LightweightSession();
        var trackerB = new MartenAggregateTracker();
        var repositoryB = new MartenEventSourcedRepository<Counter, CounterId>(sessionB, trackerB);
        var counterB = await repositoryB.GetByIdAsync(id, TestContext.Current.CancellationToken);

        counterA!.Increment(1);
        await Commit(sessionA, trackerA);

        counterB!.Increment(1);
        var exception = await Record.ExceptionAsync(() => Commit(sessionB, trackerB));

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<ConcurrencyException>(exception);
    }

    private static async Task Commit(IDocumentSession session, MartenAggregateTracker tracker)
    {
        var unitOfWork = new MartenUnitOfWork(
            session,
            tracker,
            Substitute.For<IMartenOutbox>(),
            new DomainEventEnvelopeSerializer(new DomainEventTypeRegistry([typeof(CounterCreated).Assembly])),
            new StoppedClock(CommitTime));
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);
    }

    private static async Task Seed(IDocumentStore store, CounterId id, int increments)
    {
        await using var session = store.LightweightSession();
        var tracker = new MartenAggregateTracker();
        var repository = new MartenEventSourcedRepository<Counter, CounterId>(session, tracker);
        var counter = Counter.Create(id);
        counter.Increment(increments);
        await repository.AddAsync(counter, TestContext.Current.CancellationToken);
        await Commit(session, tracker);
    }

    private DocumentStore BuildStore() =>
        DocumentStore.For(options =>
        {
            options.Connection(fixture.ConnectionString);
            options.Events.StreamIdentity = StreamIdentity.AsString;
            options.DatabaseSchemaName = "counters_" + Guid.NewGuid().ToString("N")[..8];
        });

    private sealed class StoppedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }
}

internal readonly record struct CounterId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;
}

[EventName("counter-created-v1")]
internal sealed record CounterCreated(CounterId CounterId) : DomainEvent;

[EventName("counter-incremented-v1")]
internal sealed record CounterIncremented(CounterId CounterId, int By) : DomainEvent;

internal sealed record CounterState(CounterId Id, int Total) : AggregateState<CounterState, CounterId>
{
    public static CounterState Empty => new(new CounterId(Guid.Empty), 0);

    public override CounterState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        CounterCreated created => this with { Id = created.CounterId },
        CounterIncremented incremented => this with { Id = incremented.CounterId, Total = Total + incremented.By },
        _ => this,
    };
}

[AggregateName("counter")]
internal sealed class Counter : EventSourcedAggregateRoot<CounterId, CounterState>, IReconstitutable<Counter>
{
    private Counter() : base(CounterState.Empty)
    {
    }

    public int Total => State.Total;

    static Counter IReconstitutable<Counter>.CreateEmpty() => new();

    public static Counter Create(CounterId id)
    {
        var counter = new Counter();
        counter.RaiseEvent(new CounterCreated(id));
        return counter;
    }

    public void Increment(int by) => RaiseEvent(new CounterIncremented(Id, by));
}
