using GaWeCodes.Application.Persistence;

namespace GaWeCodes.Persistence;

internal sealed class NullUnitOfWork : IUnitOfWork
{
    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
