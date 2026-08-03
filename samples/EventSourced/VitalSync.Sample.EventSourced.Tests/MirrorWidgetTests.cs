using BuildingBlocks.Application;
using NSubstitute;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Domain;
using VitalSync.Sample.EventSourced.Infrastructure.Integration;

namespace VitalSync.Sample.EventSourced.Tests;

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
            new WidgetCreatedIntegrationEvent(widgetId, "from-state-stored", Guid.NewGuid(), DateTimeOffset.UtcNow),
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

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            WidgetCreatedConsumer.Handle(
                new WidgetCreatedIntegrationEvent(Guid.NewGuid(), "doomed", Guid.NewGuid(), DateTimeOffset.UtcNow),
                sender,
                TestContext.Current.CancellationToken));
    }
}
