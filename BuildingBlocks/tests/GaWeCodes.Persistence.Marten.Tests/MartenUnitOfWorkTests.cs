using GaWeCodes.Core.Messaging.DomainEvents;
using GaWeCodes.Core.Persistence;
using GaWeCodes.Domain;
using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Persistence.Marten;
using Marten;
using NSubstitute;
using Wolverine.Marten;

namespace GaWeCodes.Tests;

public sealed class MartenUnitOfWorkTests
{
    private static readonly DateTimeOffset CommitTime = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    private static readonly DomainEventEnvelopeSerializer Serializer =
        new(new DomainEventTypeRegistry([typeof(CounterCreated).Assembly]));

    private static readonly DomainEventEnvelopeFactory EnvelopeFactory =
        new(Serializer, new StoppedClock(CommitTime));

    [Fact]
    public async Task Commit_EnrollsOutboxBeforeSaving()
    {
        var calls = new List<string>();
        var session = Substitute.For<IDocumentSession>();
        var outbox = Substitute.For<IMartenOutbox>();
        outbox.When(o => o.Enroll(session)).Do(_ => calls.Add("enroll"));
        session.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask)
            .AndDoes(_ => calls.Add("save"));

        var unitOfWork = new MartenUnitOfWork(session, TrackerWith(out _), outbox, EnvelopeFactory);
        await unitOfWork.CommitAsync(CancellationToken.None);

        Assert.Equal(["enroll", "save"], calls);
    }

    [Fact]
    public async Task Commit_PublishesEveryDomainEventToOutboxBeforeSaving()
    {
        var calls = new List<string>();
        var session = Substitute.For<IDocumentSession>();
        var outbox = Substitute.For<IMartenOutbox>();
#pragma warning disable CA2012
        outbox.When(o => o.PublishAsync(Arg.Any<object>())).Do(_ => calls.Add("publish"));
#pragma warning restore CA2012
        session.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask)
            .AndDoes(_ => calls.Add("save"));

        var tracker = TrackerWith(out var counter);
        counter.Increment(1);

        var unitOfWork = new MartenUnitOfWork(session, tracker, outbox, EnvelopeFactory);
        await unitOfWork.CommitAsync(CancellationToken.None);

        Assert.Equal(["publish", "publish", "save"], calls);
    }

    [Fact]
    public async Task Commit_AfterSuccessfulSave_ClearsTrackedEvents()
    {
        var session = Substitute.For<IDocumentSession>();
        var tracker = TrackerWith(out var counter);

        var unitOfWork = new MartenUnitOfWork(session, tracker, Substitute.For<IMartenOutbox>(), EnvelopeFactory);
        await unitOfWork.CommitAsync(CancellationToken.None);

        Assert.Empty(counter.DomainEvents);
        Assert.Empty(tracker.Entries);
    }

    [Fact]
    public async Task Commit_WhenSaveFails_KeepsTrackedEvents()
    {
        var session = Substitute.For<IDocumentSession>();
        session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("commit failed")));
        var tracker = TrackerWith(out var counter);

        var unitOfWork = new MartenUnitOfWork(session, tracker, Substitute.For<IMartenOutbox>(), EnvelopeFactory);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => unitOfWork.CommitAsync(CancellationToken.None));

        Assert.NotEmpty(counter.DomainEvents);
        Assert.NotEmpty(tracker.Entries);
    }

    [Fact]
    public async Task Commit_NumbersPublishedEnvelopesConsecutivelyUpToTheAggregateVersion()
    {
        var published = new List<DomainEventEnvelope>();
        var session = Substitute.For<IDocumentSession>();
        var outbox = Substitute.For<IMartenOutbox>();
#pragma warning disable CA2012
        outbox.When(o => o.PublishAsync(Arg.Any<DomainEventEnvelope>()))
            .Do(call => published.Add(call.Arg<DomainEventEnvelope>()!));
#pragma warning restore CA2012

        var tracker = TrackerWith(out var counter);
        counter.Increment(1);
        counter.Increment(2);

        var unitOfWork = new MartenUnitOfWork(session, tracker, outbox, EnvelopeFactory);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Equal([1L, 2L, 3L], published.Select(envelope => envelope.Version));
        Assert.Equal(((IStateOwner)counter).Version, published[^1].Version);
    }

    private static MartenAggregateTracker TrackerWith(out Counter counter)
    {
        var tracker = new MartenAggregateTracker();
        var aggregate = Counter.Create(new CounterId(Guid.NewGuid()));
        tracker.Track(aggregate, "counter", aggregate.Id.Value.ToString(), () => ((IStateOwner)aggregate).Version);
        counter = aggregate;
        return tracker;
    }

    private sealed class StoppedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }
}

