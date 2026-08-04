namespace BuildingBlocks.Application;

public interface ISender
{
    Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken);

    Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);

    Task<Result<TResult>> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
