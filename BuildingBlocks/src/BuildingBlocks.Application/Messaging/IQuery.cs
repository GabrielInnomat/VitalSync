using BuildingBlocks.Common.Results;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>Represents a read-only query returning a value-bearing result.</summary>
/// <typeparam name="TResponse">The produced value type.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}