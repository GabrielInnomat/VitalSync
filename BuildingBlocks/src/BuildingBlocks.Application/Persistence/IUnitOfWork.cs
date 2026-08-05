namespace BuildingBlocks.Application.Persistence;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
}
