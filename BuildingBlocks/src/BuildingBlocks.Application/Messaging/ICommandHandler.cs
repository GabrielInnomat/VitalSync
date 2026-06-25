using BuildingBlocks.Common.Results;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>Handles a command of type <typeparamref name="TCommand"/>.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>Handles a command of type <typeparamref name="TCommand"/> producing a value.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The produced value type.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}