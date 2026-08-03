using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class OutboxFlushOnCommitTests(PostgreSqlFixture fixture)
{
    private static readonly TimeSpan DeliveryTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task MartenCommit_FlushesOutboxToProjectionWithoutDurabilityAgent()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBuildingBlocks(options => options.UseMartenEventSourcing(fixture.ConnectionString));
                services.AddScoped<ICommandHandler<CreateFlushCounter>, CreateFlushCounterHandler>();
                services.AddScoped<IProjectionHandler<FlushCounterCreated>, FlushCounterProjection>();
                services.AddSingleton<FlushDeliverySignal>();
            })
            .UseWolverine(ConfigureFlushOnlyDurability)
            .StartAsync(TestContext.Current.CancellationToken);

        var id = Guid.NewGuid();
        using (var scope = host.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new CreateFlushCounter(id), TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
        }

        var delivered = await host.Services.GetRequiredService<FlushDeliverySignal>()
            .Delivered.WaitAsync(DeliveryTimeout, TestContext.Current.CancellationToken);
        var created = Assert.IsType<FlushCounterCreated>(delivered);
        Assert.Equal(id, created.CounterId.Value);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EfCoreCommit_FlushesOutboxToProjectionWithoutDurabilityAgent()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        var builder = Host.CreateApplicationBuilder();

        builder.AddBuildingBlocks(
            options => options.UseEfCorePersistence<FlushProbeContext>(fixture.ConnectionString),
            ConfigureFlushOnlyDurability);

        builder.Services.AddScoped<ICommandHandler<StartFlushProbe>, StartFlushProbeHandler>();
        builder.Services.AddScoped<IProjectionHandler<FlushProbeStarted>, FlushProbeProjection>();
        builder.Services.AddSingleton<FlushDeliverySignal>();

        using var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var id = Guid.NewGuid();
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<FlushProbeContext>();
            await context.Database.ExecuteSqlRawAsync(
                "create table if not exists flush_probe_rows (id uuid primary key, name text not null)",
                TestContext.Current.CancellationToken);

            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new StartFlushProbe(id), TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
        }

        var delivered = await host.Services.GetRequiredService<FlushDeliverySignal>()
            .Delivered.WaitAsync(DeliveryTimeout, TestContext.Current.CancellationToken);
        var started = Assert.IsType<FlushProbeStarted>(delivered);
        Assert.Equal(id, started.ProbeId.Value);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private static void ConfigureFlushOnlyDurability(WolverineOptions options)
    {
        options.Durability.Mode = DurabilityMode.Solo;

        options.Durability.ScheduledJobFirstExecution = TimeSpan.FromHours(1);
        options.Durability.ScheduledJobPollingTime = TimeSpan.FromHours(1);

        options.ApplicationAssembly = typeof(DomainEventEnvelopeHandler).Assembly;
    }
}

public sealed class FlushDeliverySignal
{
    private readonly TaskCompletionSource<IDomainEvent> _delivered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<IDomainEvent> Delivered => _delivered.Task;

    public void MarkDelivered(IDomainEvent domainEvent) => _delivered.TrySetResult(domainEvent);
}

public sealed record CreateFlushCounter(Guid Id) : ICommand;

public sealed class CreateFlushCounterHandler(IRepository<FlushCounter, FlushCounterId> repository)
    : ICommandHandler<CreateFlushCounter>
{
    public async Task<Result> Handle(CreateFlushCounter command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await repository.AddAsync(FlushCounter.Create(new FlushCounterId(command.Id)), cancellationToken);
        return Result.Success();
    }
}

public sealed class FlushCounterProjection(FlushDeliverySignal signal) : IProjectionHandler<FlushCounterCreated>
{
    public Task Handle(FlushCounterCreated domainEvent, CancellationToken cancellationToken)
    {
        signal.MarkDelivered(domainEvent);
        return Task.CompletedTask;
    }
}

public readonly record struct FlushCounterId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;
}

public sealed record FlushCounterCreated(FlushCounterId CounterId) : DomainEvent;

public sealed record FlushCounterState(FlushCounterId Id) : IState<FlushCounterState, FlushCounterId>
{
    public static FlushCounterState Empty => new(new FlushCounterId(Guid.Empty));

    public FlushCounterState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        FlushCounterCreated created => this with { Id = created.CounterId },
        _ => this,
    };
}

public sealed class FlushCounter : EventSourcedAggregateRoot<FlushCounterId, FlushCounterState>, IReconstitutable<FlushCounter>
{
    private FlushCounter() : base(FlushCounterState.Empty)
    {
    }

    static FlushCounter IReconstitutable<FlushCounter>.CreateEmpty() => new();

    public static FlushCounter Create(FlushCounterId id)
    {
        var counter = new FlushCounter();
        counter.RaiseEvent(new FlushCounterCreated(id));
        return counter;
    }
}

public sealed record StartFlushProbe(Guid Id) : ICommand;

public sealed class StartFlushProbeHandler(IRepository<FlushProbe, FlushProbeId> repository)
    : ICommandHandler<StartFlushProbe>
{
    public async Task<Result> Handle(StartFlushProbe command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await repository.AddAsync(FlushProbe.Create(new FlushProbeId(command.Id)), cancellationToken);
        return Result.Success();
    }
}

public sealed class FlushProbeProjection(FlushDeliverySignal signal) : IProjectionHandler<FlushProbeStarted>
{
    public Task Handle(FlushProbeStarted domainEvent, CancellationToken cancellationToken)
    {
        signal.MarkDelivered(domainEvent);
        return Task.CompletedTask;
    }
}

public sealed record FlushProbeStarted(FlushProbeId ProbeId) : DomainEvent;

public sealed record FlushProbeRenamed(FlushProbeId ProbeId, string Name) : DomainEvent;

public sealed record RenameFlushProbe(Guid Id, string Name) : ICommand;

public sealed class RenameFlushProbeHandler(IRepository<FlushProbe, FlushProbeId> repository)
    : ICommandHandler<RenameFlushProbe>
{
    public async Task<Result> Handle(RenameFlushProbe command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var probe = await repository.GetByIdAsync(new FlushProbeId(command.Id), cancellationToken);
        if (probe is null)
        {
            return Failure.NotFound("probe.not_found", "No probe with that id exists.");
        }

        probe.Rename(command.Name);
        return Result.Success();
    }
}

public readonly record struct FlushProbeId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;
}

public sealed record FlushProbeState(FlushProbeId Id, string Name) : IState<FlushProbeState, FlushProbeId>
{
    public static FlushProbeState Empty => new(default, string.Empty);

    public FlushProbeState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        FlushProbeStarted started => this with { Id = started.ProbeId, Name = "probe" },
        FlushProbeRenamed renamed => this with { Name = renamed.Name },
        _ => this,
    };
}

public sealed class FlushProbe : AggregateRoot<FlushProbeId, FlushProbeState>, IReconstitutable<FlushProbe>
{
    private FlushProbe() : base(FlushProbeState.Empty)
    {
    }

    public string Name => State.Name;

    static FlushProbe IReconstitutable<FlushProbe>.CreateEmpty() => new();

    public static FlushProbe Create(FlushProbeId id)
    {
        var probe = new FlushProbe();
        probe.RaiseEvent(new FlushProbeStarted(id));
        return probe;
    }

    public void Rename(string name) => RaiseEvent(new FlushProbeRenamed(Id, name));
}

public sealed class FlushProbeContext(DbContextOptions<FlushProbeContext> options) : DbContext(options)
{
    public DbSet<FlushProbeState> Probes => Set<FlushProbeState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<FlushProbeState>(entity =>
        {
            entity.ToTable("flush_probe_rows");
            entity.HasKey(state => state.Id);
            entity.Property(state => state.Id).HasColumnName("id");
            entity.Property(state => state.Name).HasColumnName("name");
        });

        modelBuilder.ApplyEntityKeyConversions();
    }
}
