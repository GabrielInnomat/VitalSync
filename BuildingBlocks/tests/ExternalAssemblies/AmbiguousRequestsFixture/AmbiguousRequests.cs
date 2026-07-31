using BuildingBlocks.Application;

namespace AmbiguousRequestsFixture;

public sealed record AmbiguousQuery : IQuery<int>, IQuery<string>;

public sealed class AmbiguousQueryIntHandler : IQueryHandler<AmbiguousQuery, int>
{
    public Task<Result<int>> Handle(AmbiguousQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(1));
}

public sealed class AmbiguousQueryStringHandler : IQueryHandler<AmbiguousQuery, string>
{
    public Task<Result<string>> Handle(AmbiguousQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success("one"));
}
