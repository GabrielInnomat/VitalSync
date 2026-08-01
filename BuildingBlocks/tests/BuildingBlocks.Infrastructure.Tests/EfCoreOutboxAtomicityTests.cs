using System.Collections.Concurrent;
using System.Data.Common;
using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

// ADR-0022's central promise is that aggregate state and the outbox entry commit together or not at all.
// That promise was only ever observed in a log; this pins it. If a future change moves the envelope write
// out of the context's SaveChanges - a second connection, an explicit send, an eager flush - the two
// statements stop sharing a command and this test fails.
[Collection(PostgreSqlCollection.Name)]
public sealed class EfCoreOutboxAtomicityTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task AggregateAndOutboxEntry_ArePersistedByOneCommand()
    {
        Assert.SkipUnless(fixture.Available, fixture.SkipReason);

        var recorder = new CommandRecorder();

        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBuildingBlocks(options =>
                    options.UseEfCorePersistence<FlushProbeContext>(
                        fixture.ConnectionString,
                        builder => builder.AddInterceptors(recorder)));

                services.AddScoped<ICommandHandler<StartFlushProbe>, StartFlushProbeHandler>();
            })
            .UseWolverine(options =>
            {
                options.Durability.Mode = DurabilityMode.Solo;
                options.ApplicationAssembly = typeof(DomainEventEnvelopeHandler).Assembly;
                options.UseBuildingBlocksEfCorePersistence(fixture.ConnectionString);
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using (var scope = host.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<FlushProbeContext>().Database.ExecuteSqlRawAsync(
                "create table if not exists flush_probe_rows (id uuid primary key, name text not null)",
                TestContext.Current.CancellationToken);
        }

        recorder.Clear();

        using (var scope = host.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(
                new StartFlushProbe(Guid.NewGuid()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
        }

        var combined = recorder.Commands
            .Where(sql => sql.Contains("flush_probe_rows", StringComparison.OrdinalIgnoreCase))
            .Where(sql => sql.Contains("wolverine", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            combined.Count == 1,
            "The aggregate row and the outbox envelope must be written by a single command so they share one " +
            $"transaction. Commands touching both: {combined.Count}. All recorded commands:{Environment.NewLine}" +
            string.Join(Environment.NewLine + "---" + Environment.NewLine, recorder.Commands));

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        private readonly ConcurrentQueue<string> _commands = new();

        public IReadOnlyList<string> Commands => [.. _commands];

        public void Clear() => _commands.Clear();

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void Record(DbCommand command) => _commands.Enqueue(command.CommandText);
    }
}
