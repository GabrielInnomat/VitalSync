using BuildingBlocks.Common.Results;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>Represents a command that performs a write and returns a <see cref="Result"/>.</summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>Represents a command that performs a write and returns a value-bearing result.</summary>
/// <typeparam name="TResponse">The type of the produced value.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}