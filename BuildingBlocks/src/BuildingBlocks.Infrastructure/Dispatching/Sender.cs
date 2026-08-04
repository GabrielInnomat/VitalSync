using System.Collections.Concurrent;
using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Dispatching;

internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private readonly record struct DispatcherKey(Type Request, Type Result);

    private static readonly ConcurrentDictionary<Type, CommandDispatcher> CommandDispatchers = new();
    private static readonly ConcurrentDictionary<DispatcherKey, object> CommandWithResultDispatchers = new();
    private static readonly ConcurrentDictionary<DispatcherKey, object> QueryDispatchers = new();

    public Task<Result> Send(ICommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = CommandDispatchers.GetOrAdd(
            command.GetType(),
            static type => (CommandDispatcher)Activator.CreateInstance(
                typeof(CommandDispatcher<>).MakeGenericType(type))!);

        return dispatcher.Dispatch(command, serviceProvider, cancellationToken);
    }

    public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = (CommandWithResultDispatcher<TResult>)CommandWithResultDispatchers.GetOrAdd(
            new DispatcherKey(command.GetType(), typeof(TResult)),
            static key => Activator.CreateInstance(
                typeof(CommandWithResultDispatcher<,>).MakeGenericType(key.Request, key.Result))!);

        return dispatcher.Dispatch(command, serviceProvider, cancellationToken);
    }

    public Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dispatcher = (QueryDispatcher<TResult>)QueryDispatchers.GetOrAdd(
            new DispatcherKey(query.GetType(), typeof(TResult)),
            static key => Activator.CreateInstance(
                typeof(QueryDispatcher<,>).MakeGenericType(key.Request, key.Result))!);

        return dispatcher.Dispatch(query, serviceProvider, cancellationToken);
    }

    private static RequestPipelineContinuation<TResponse> BuildPipeline<TRequest, TResponse>(
        TRequest request,
        RequestPipelineContinuation<TResponse> handler,
        IServiceProvider services)
    {
        var registry = services.GetService<PipelineBehaviorRegistry>();
        var behaviors = services.GetServices<IPipelineBehavior<TRequest, TResponse>>();
        var ordered = registry is null
            ? behaviors
            : behaviors.OrderByDescending(behavior => registry.GetOrder(behavior.GetType()));

        var pipeline = handler;
        foreach (var behavior in ordered)
        {
            var next = pipeline;
            var current = behavior;
            pipeline = cancellationToken => current.Handle(request, next, cancellationToken);
        }

        return pipeline;
    }

    private abstract class CommandDispatcher
    {
        public abstract Task<Result> Dispatch(ICommand command, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class CommandDispatcher<TCommand> : CommandDispatcher
        where TCommand : ICommand
    {
        public override Task<Result> Dispatch(ICommand command, IServiceProvider services, CancellationToken cancellationToken)
        {
            var typedCommand = (TCommand)command;
            var handler = services.GetRequiredService<ICommandHandler<TCommand>>();
            var pipeline = BuildPipeline<TCommand, Result>(
                typedCommand,
                ct => handler.Handle(typedCommand, ct),
                services);
            return pipeline(cancellationToken);
        }
    }

    private abstract class CommandWithResultDispatcher<TResult>
    {
        public abstract Task<Result<TResult>> Dispatch(ICommand<TResult> command, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class CommandWithResultDispatcher<TCommand, TResult> : CommandWithResultDispatcher<TResult>
        where TCommand : ICommand<TResult>
    {
        public override Task<Result<TResult>> Dispatch(ICommand<TResult> command, IServiceProvider services, CancellationToken cancellationToken)
        {
            var typedCommand = (TCommand)command;
            var handler = services.GetRequiredService<ICommandHandler<TCommand, TResult>>();
            var pipeline = BuildPipeline<TCommand, Result<TResult>>(
                typedCommand,
                ct => handler.Handle(typedCommand, ct),
                services);
            return pipeline(cancellationToken);
        }
    }

    private abstract class QueryDispatcher<TResult>
    {
        public abstract Task<Result<TResult>> Dispatch(IQuery<TResult> query, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class QueryDispatcher<TQuery, TResult> : QueryDispatcher<TResult>
        where TQuery : IQuery<TResult>
    {
        public override Task<Result<TResult>> Dispatch(IQuery<TResult> query, IServiceProvider services, CancellationToken cancellationToken)
        {
            var typedQuery = (TQuery)query;
            var handler = services.GetRequiredService<IQueryHandler<TQuery, TResult>>();
            var pipeline = BuildPipeline<TQuery, Result<TResult>>(
                typedQuery,
                ct => handler.Handle(typedQuery, ct),
                services);
            return pipeline(cancellationToken);
        }
    }
}
