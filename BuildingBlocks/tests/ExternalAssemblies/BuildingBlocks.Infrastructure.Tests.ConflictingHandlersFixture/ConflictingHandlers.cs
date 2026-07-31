using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Tests.ConflictingHandlersFixture;

public sealed record ConflictingCommand : ICommand;

public sealed class FirstConflictingCommandHandler : ICommandHandler<ConflictingCommand>
{
    public Task<Result> Handle(ConflictingCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}

public sealed class SecondConflictingCommandHandler : ICommandHandler<ConflictingCommand>
{
    public Task<Result> Handle(ConflictingCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}
