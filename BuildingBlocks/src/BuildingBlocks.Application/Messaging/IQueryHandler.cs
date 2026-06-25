using BuildingBlocks.Common.Results;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>Handles a query of type <typeparamref name="TQuery"/>.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The produced value type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}