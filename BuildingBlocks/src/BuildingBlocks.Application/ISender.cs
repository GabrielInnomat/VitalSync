namespace BuildingBlocks.Application;

public interface ISender
{
    Task<Result> Send(ICommand command, CancellationToken cancellationToken);

    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);

    Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
