using System.Collections.Concurrent;
using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Dispatching;

/// <summary>
/// DI-based implementation of <see cref="ISender"/> that dispatches commands and queries through the behavior pipeline.
/// </summary>
/// <remarks>
/// For each request the sender resolves the single matching handler and the registered
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/>s from the container and wraps the handler in the behavior
/// chain; behaviors execute in the explicit order recorded by <see cref="PipelineBehaviorRegistry"/> (ADR-0015) — lower
/// orders wrap further out and execute earlier. Dispatch avoids reflection-heavy scanning:
/// the closed-generic dispatcher for each request type is created once and cached for subsequent sends. Register the
/// sender as a scoped service via <c>AddBuildingBlocks</c> so handlers resolve from the current scope.
/// </remarks>
/// <param name="serviceProvider">The service provider used to resolve handlers and pipeline behaviors.</param>
public sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, CommandDispatcher> CommandDispatchers = new();
    private static readonly ConcurrentDictionary<Type, object> CommandWithResultDispatchers = new();
    private static readonly ConcurrentDictionary<Type, object> QueryDispatchers = new();

    /// <inheritdoc/>
    public Task<Result> Send(ICommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = CommandDispatchers.GetOrAdd(
            command.GetType(),
            static type => (CommandDispatcher)Activator.CreateInstance(
                typeof(CommandDispatcher<>).MakeGenericType(type))!);

        return dispatcher.Dispatch(command, serviceProvider, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = (CommandWithResultDispatcher<TResult>)CommandWithResultDispatchers.GetOrAdd(
            command.GetType(),
            static type => Activator.CreateInstance(
                typeof(CommandWithResultDispatcher<,>).MakeGenericType(type, typeof(TResult)))!);

        return dispatcher.Dispatch(command, serviceProvider, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dispatcher = (QueryDispatcher<TResult>)QueryDispatchers.GetOrAdd(
            query.GetType(),
            static type => Activator.CreateInstance(
                typeof(QueryDispatcher<,>).MakeGenericType(type, typeof(TResult)))!);

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
