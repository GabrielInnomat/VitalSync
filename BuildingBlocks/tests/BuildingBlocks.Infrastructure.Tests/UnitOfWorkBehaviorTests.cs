using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.Dispatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class UnitOfWorkBehaviorTests
{
    [Fact]
    public async Task SuccessfulCommand_CommitsExactlyOnce()
    {
        var unitOfWork = new RecordingUnitOfWork();
        using var provider = BuildProvider(unitOfWork, new PassingCommandHandler());
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new ProbeCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task FailedCommand_DoesNotCommit()
    {
        var unitOfWork = new RecordingUnitOfWork();
        using var provider = BuildProvider(unitOfWork, new FailingCommandHandler());
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new ProbeCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task Query_DoesNotCommit()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IUnitOfWork>(_ => unitOfWork);
        services.AddScoped<IQueryHandler<ProbeQuery, int>, ProbeQueryHandler>();
        services.AddBuildingBlocks(_ => { });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new ProbeQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task EfCoreConcurrencyConflictOnCommit_IsMappedToConflictFailure()
    {
        var unitOfWork = new ThrowingUnitOfWork(new DbUpdateConcurrencyException("row changed"));
        using var provider = BuildProvider(unitOfWork, new PassingCommandHandler());
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new ProbeCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(FailureCategory.Conflict, failure.Category);
        Assert.Equal(UnitOfWorkBehavior<ProbeCommand, Result>.ConcurrencyConflictCode, failure.Code);
    }

    [Fact]
    public async Task SuccessfulCommand_WithoutRegisteredUnitOfWork_PassesThrough()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ICommandHandler<ProbeCommand>, PassingCommandHandler>();
        services.AddBuildingBlocks(_ => { });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new ProbeCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Behavior_InstantiatedDirectlyWithoutUnitOfWork_PassesThrough()
    {
        var behavior = new UnitOfWorkBehavior<ProbeCommand, Result>();

        var result = await behavior.HandleAsync(
            new ProbeCommand(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void MissingUnitOfWork_LogsStartupNotice()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddBuildingBlocks(_ => { });

        using var provider = services.BuildServiceProvider();

        RunUnitOfWorkPresenceCheck(provider);

        Assert.Contains(NoticeRecords(provider), record => record.Level == LogLevel.Information);
    }

    [Fact]
    public void RegisteredUnitOfWork_DoesNotLogStartupNotice()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddScoped<IUnitOfWork>(_ => new RecordingUnitOfWork());
        services.AddBuildingBlocks(_ => { });

        using var provider = services.BuildServiceProvider();

        RunUnitOfWorkPresenceCheck(provider);

        Assert.Empty(NoticeRecords(provider));
    }

    [Fact]
    public void UnitOfWorkRegisteredAfterBuildingBlocks_DoesNotLogStartupNotice()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddBuildingBlocks(_ => { });
        services.AddScoped<IUnitOfWork>(_ => new RecordingUnitOfWork());

        using var provider = services.BuildServiceProvider();

        RunUnitOfWorkPresenceCheck(provider);

        Assert.Empty(NoticeRecords(provider));
    }

    private static void RunUnitOfWorkPresenceCheck(ServiceProvider provider) =>
        Assert.Single(
            provider.GetServices<IStartupCheck>(),
            check => check is UnitOfWorkPresenceCheck).Run();

    private static IEnumerable<FakeLogRecord> NoticeRecords(ServiceProvider provider) =>
        provider.GetRequiredService<FakeLogCollector>()
            .GetSnapshot()
            .Where(record => record.Message.Contains("No persistence configured", StringComparison.Ordinal));

    private static ServiceProvider BuildProvider(IUnitOfWork unitOfWork, ICommandHandler<ProbeCommand> handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IUnitOfWork>(_ => unitOfWork);
        services.AddScoped<ICommandHandler<ProbeCommand>>(_ => handler);
        services.AddBuildingBlocks(_ => { });
        return services.BuildServiceProvider();
    }

    private sealed record ProbeCommand : ICommand;

    private sealed record ProbeQuery : IQuery<int>;

    private sealed class PassingCommandHandler : ICommandHandler<ProbeCommand>
    {
        public Task<Result> HandleAsync(ProbeCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FailingCommandHandler : ICommandHandler<ProbeCommand>
    {
        public Task<Result> HandleAsync(ProbeCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure(Failure.NotFound("probe.not_found", "Nothing here.")));
    }

    private sealed class ProbeQueryHandler : IQueryHandler<ProbeQuery, int>
    {
        public Task<Result<int>> HandleAsync(ProbeQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(42));
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int CommitCount { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            CommitCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingUnitOfWork(Exception exception) : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken cancellationToken) => throw exception;
    }
}
