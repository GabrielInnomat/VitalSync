using BuildingBlocks.Application.Persistence;

namespace BuildingBlocks.Infrastructure.Persistence;

internal sealed class NullUnitOfWork : IUnitOfWork
{
    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
