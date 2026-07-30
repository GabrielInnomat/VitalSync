using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Tests.ConflictingHandlers;

// This assembly deliberately contains two handlers for the same command so that a
// scan of it exercises the duplicate-handler guard in AddHandlersFrom (IMP-05). It
// is intentionally isolated in its own assembly: placing these types in the main
// test assembly would make every AddHandlersFrom(thisAssembly) scan throw.
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
