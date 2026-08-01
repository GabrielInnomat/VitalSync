using BuildingBlocks.Application;
using NSubstitute;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Domain;
using VitalSync.Sample.EventSourced.Infrastructure.Integration;

namespace VitalSync.Sample.EventSourced.Tests;

// Delivery across the service boundary is at-least-once, so the same integration event will eventually be
// handled twice. Nothing in the transport prevents that; only this handler does.
public sealed class MirrorWidgetTests
{
    private readonly IRepository<Gadget, GadgetId> _repository = Substitute.For<IRepository<Gadget, GadgetId>>();

    [Fact]
    public async Task Mirroring_AnUnknownWidget_CreatesAGadgetUnderTheSameIdentity()
    {
        var widgetId = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Any<GadgetId>(), Arg.Any<CancellationToken>()).Returns((Gadget?)null);

        var result = await new MirrorWidgetHandler(_repository)
            .Handle(new MirrorWidget(widgetId, "mirrored"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        // The shared identifier is the whole idempotency mechanism - it must not be regenerated here.
        await _repository.Received(1).AddAsync(
            Arg.Is<Gadget>(gadget => gadget.Id == new GadgetId(widgetId) && gadget.Name == "mirrored"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mirroring_AnAlreadyMirroredWidget_SucceedsWithoutWritingAgain()
    {
        var widgetId = Guid.NewGuid();
        _repository.GetByIdAsync(new GadgetId(widgetId), Arg.Any<CancellationToken>())
            .Returns(Gadget.Create(new GadgetId(widgetId), "mirrored"));

        var result = await new MirrorWidgetHandler(_repository)
            .Handle(new MirrorWidget(widgetId, "mirrored"), TestContext.Current.CancellationToken);

        // Success, not a conflict: the message has been dealt with, and failing would make the broker retry a
        // fact that is already true.
        Assert.True(result.IsSuccess);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Gadget>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TheSubscribingHandler_TranslatesTheContractIntoACommand()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ICommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var widgetId = Guid.NewGuid();

        await WidgetCreatedConsumer.Handle(
            new WidgetCreatedIntegrationEvent(widgetId, "from-state-stored"),
            sender,
            TestContext.Current.CancellationToken);

        await sender.Received(1).Send(
            Arg.Is<ICommand>(command =>
                ((MirrorWidget)command).WidgetId == widgetId
                && ((MirrorWidget)command).Name == "from-state-stored"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TheSubscribingHandler_TurnsAFailedCommandIntoAnException()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ICommand>(), Arg.Any<CancellationToken>())
            .Returns(Failure.Conflict("gadget.conflict", "someone else got there first"));

        // Returning normally would acknowledge the message and lose it. Throwing is what hands it back to
        // Wolverine's retry and dead-letter policy - the one thing this thin handler has to get right.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            WidgetCreatedConsumer.Handle(
                new WidgetCreatedIntegrationEvent(Guid.NewGuid(), "doomed"),
                sender,
                TestContext.Current.CancellationToken));
    }
}
