using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class PipelineOrderingTests
{
    [Fact]
    public async Task Behaviors_ExecuteInAscendingOrder_LowerWrapsFurtherOut()
    {
        var recorder = new ExecutionRecorder();
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddSingleton(recorder);
        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
        services.AddScoped<ICommandHandler<ProbeCommand>, ProbeCommandHandler>();

        services.AddBuildingBlocks(options =>
        {
            options.AddPipelineBehavior(typeof(InnerBehavior<,>), 400);
            options.AddPipelineBehavior(typeof(OuterBehavior<,>), -100);
            options.AddPipelineBehavior(typeof(MiddleBehavior<,>), 200);
        });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ProbeCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["outer", "middle", "inner", "handler"], recorder.Entries);
    }

    [Fact]
    public async Task ExpectedDomainException_IsLoggedAsWarning_NotError()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddScoped<ICommandHandler<ThrowingCommand>, ThrowingCommandHandler>();
        services.AddBuildingBlocks(_ => { });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ThrowingCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);

        var records = provider.GetRequiredService<FakeLogCollector>().GetSnapshot();
        Assert.Contains(records, record => record.Level == LogLevel.Warning);
        Assert.DoesNotContain(records, record => record.Level == LogLevel.Error);
    }

    private sealed record ProbeCommand : ICommand;

    private sealed record ThrowingCommand : ICommand;

    private sealed class ExecutionRecorder
    {
        public List<string> Entries { get; } = [];
    }

    private sealed class ProbeCommandHandler(ExecutionRecorder recorder) : ICommandHandler<ProbeCommand>
    {
        public Task<Result> Handle(ProbeCommand command, CancellationToken cancellationToken)
        {
            recorder.Entries.Add("handler");
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ThrowingCommandHandler : ICommandHandler<ThrowingCommand>
    {
        public Task<Result> Handle(ThrowingCommand command, CancellationToken cancellationToken) =>
            throw new DomainValidationException("Name must not be empty.");
    }

    private abstract class RecordingBehavior<TRequest, TResponse>(ExecutionRecorder recorder, string name)
        : IPipelineBehavior<TRequest, TResponse>
        where TResponse : Result
    {
        public Task<TResponse> Handle(TRequest request, RequestPipelineContinuation<TResponse> continuation, CancellationToken cancellationToken)
        {
            recorder.Entries.Add(name);
            return continuation(cancellationToken);
        }
    }

    private sealed class OuterBehavior<TRequest, TResponse>(ExecutionRecorder recorder)
        : RecordingBehavior<TRequest, TResponse>(recorder, "outer")
        where TResponse : Result;

    private sealed class MiddleBehavior<TRequest, TResponse>(ExecutionRecorder recorder)
        : RecordingBehavior<TRequest, TResponse>(recorder, "middle")
        where TResponse : Result;

    private sealed class InnerBehavior<TRequest, TResponse>(ExecutionRecorder recorder)
        : RecordingBehavior<TRequest, TResponse>(recorder, "inner")
        where TResponse : Result;
}
