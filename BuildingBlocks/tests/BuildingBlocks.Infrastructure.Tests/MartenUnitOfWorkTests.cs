using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Persistence;
using Marten;
using NSubstitute;
using Wolverine.Marten;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class MartenUnitOfWorkTests
{
    private static readonly DateTimeOffset CommitTime = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    private static readonly DomainEventEnvelopeSerializer Serializer =
        new(new DomainEventTypeRegistry([typeof(CounterCreated).Assembly]));

    [Fact]
    public async Task Commit_EnrollsOutboxBeforeSaving()
    {
        var calls = new List<string>();
        var session = Substitute.For<IDocumentSession>();
        var outbox = Substitute.For<IMartenOutbox>();
        outbox.When(o => o.Enroll(session)).Do(_ => calls.Add("enroll"));
        session.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask)
            .AndDoes(_ => calls.Add("save"));

        var unitOfWork = new MartenUnitOfWork(session, TrackerWith(out _), outbox, Serializer, new StoppedClock(CommitTime));
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

        var unitOfWork = new MartenUnitOfWork(session, tracker, outbox, Serializer, new StoppedClock(CommitTime));
        await unitOfWork.CommitAsync(CancellationToken.None);

        Assert.Equal(["publish", "publish", "save"], calls);
    }

    [Fact]
    public async Task Commit_AfterSuccessfulSave_ClearsTrackedEvents()
    {
        var session = Substitute.For<IDocumentSession>();
        var tracker = TrackerWith(out var counter);

        var unitOfWork = new MartenUnitOfWork(session, tracker, Substitute.For<IMartenOutbox>(), Serializer, new StoppedClock(CommitTime));
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

        var unitOfWork = new MartenUnitOfWork(session, tracker, Substitute.For<IMartenOutbox>(), Serializer, new StoppedClock(CommitTime));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => unitOfWork.CommitAsync(CancellationToken.None));

        Assert.NotEmpty(counter.DomainEvents);
        Assert.NotEmpty(tracker.Entries);
    }

    private static MartenAggregateTracker TrackerWith(out Counter counter)
    {
        var tracker = new MartenAggregateTracker();
        var aggregate = Counter.Create(new CounterId(Guid.NewGuid()));
        tracker.Track(aggregate, "counter", aggregate.Id.Value.ToString(), () => aggregate.DomainEvents.Count);
        counter = aggregate;
        return tracker;
    }

    private sealed class StoppedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }
}

