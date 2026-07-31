using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

// End-to-end regression tests for IMP-08: after a successful command, the transactional outbox is
// flushed immediately on commit — for both persistence paths — instead of waiting for the durability
// agent's polling. The agent's recovery polling is pushed out by an hour, so a prompt delivery can
// only ever come through the flush-on-commit path.
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

        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBuildingBlocks(options =>
                    options.UseEfCorePersistence<FlushProbeContext>(fixture.ConnectionString));
                services.AddScoped<ICommandHandler<StartFlushProbe>, StartFlushProbeHandler>();
                services.AddScoped<IProjectionHandler<FlushProbeStarted>, FlushProbeProjection>();
                services.AddSingleton<FlushDeliverySignal>();
            })
            .UseWolverine(ConfigureFlushOnlyDurability)
            .StartAsync(TestContext.Current.CancellationToken);

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
        Assert.Equal(id, started.ProbeId);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private static void ConfigureFlushOnlyDurability(WolverineOptions options)
    {
        options.Durability.Mode = DurabilityMode.Solo;

        // Push the durability agent's crash-recovery polling far beyond the assertion window, so a
        // prompt delivery can only come from the flush-on-commit path — never from polling.
        options.Durability.ScheduledJobFirstExecution = TimeSpan.FromHours(1);
        options.Durability.ScheduledJobPollingTime = TimeSpan.FromHours(1);

        // Keep Wolverine's conventional discovery away from this test assembly's unrelated
        // *Handler fixtures; the Building Blocks extension includes its own assembly anyway.
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

// --- Marten (event-sourced) path ---

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

public sealed class FlushCounter() : EventSourcedAggregateRoot<FlushCounterId, FlushCounterState>(FlushCounterState.Empty)
{
    public static FlushCounter Create(FlushCounterId id)
    {
        var counter = new FlushCounter();
        counter.RaiseEvent(new FlushCounterCreated(id));
        return counter;
    }
}

// --- EF Core (state-stored) path ---

public sealed record StartFlushProbe(Guid Id) : ICommand;

public sealed class StartFlushProbeHandler(FlushProbeContext context) : ICommandHandler<StartFlushProbe>
{
    public Task<Result> Handle(StartFlushProbe command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var row = new FlushProbeRow { Id = command.Id, Name = "probe" };
        row.Start();
        context.Rows.Add(row);
        return Task.FromResult(Result.Success());
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

public sealed record FlushProbeStarted(Guid ProbeId) : DomainEvent;

public sealed class FlushProbeRow : IDomainEventsManager
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Start() => _domainEvents.Add(new FlushProbeStarted(Id));

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public sealed class FlushProbeContext(DbContextOptions<FlushProbeContext> options) : DbContext(options)
{
    public DbSet<FlushProbeRow> Rows => Set<FlushProbeRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<FlushProbeRow>(entity =>
        {
            entity.ToTable("flush_probe_rows");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.Name).HasColumnName("name");
            entity.Ignore(row => row.DomainEvents);
        });
    }
}
