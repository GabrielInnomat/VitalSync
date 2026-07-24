using NSubstitute;

namespace BuildingBlocks.Application.Tests;

public sealed class SenderContractTests
{
    private sealed record DeleteRecipeCommand : ICommand;

    private sealed record CreateRecipeCommand : ICommand<int>;

    private sealed record GetRecipeQuery : IQuery<string>;

    [Fact]
    public async Task Send_Command_ReturnsHandlerResult()
    {
        var sender = Substitute.For<ISender>();
        var command = new DeleteRecipeCommand();
        sender.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await sender.Send(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Send_CommandWithResult_ReturnsHandlerValue()
    {
        var sender = Substitute.For<ISender>();
        var command = new CreateRecipeCommand();
        sender.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(99));

        var result = await sender.Send(command, CancellationToken.None);

        Assert.Equal(99, result.Value);
    }

    [Fact]
    public async Task Send_Query_ReturnsHandlerValue()
    {
        var sender = Substitute.For<ISender>();
        var query = new GetRecipeQuery();
        sender.Send(query, Arg.Any<CancellationToken>()).Returns(Result.Success("soup"));

        var result = await sender.Send(query, CancellationToken.None);

        Assert.Equal("soup", result.Value);
    }
}
