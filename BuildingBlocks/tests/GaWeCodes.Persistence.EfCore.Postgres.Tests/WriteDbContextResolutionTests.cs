using GaWeCodes.Application.Persistence;
using GaWeCodes.Domain.Events;
using GaWeCodes.DependencyInjection;
using GaWeCodes.Persistence;
using GaWeCodes.Persistence.StateStored;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GaWeCodes.Tests;

public sealed class WriteDbContextResolutionTests
{
    private const string UnusedConnectionString =
        "Host=localhost;Port=5432;Database=write_context_resolution;Username=none;Password=none";

    [Fact]
    public async Task Repository_WhenAForeignContextOwnsTheBareDbContextKey_StillWritesToTheWriteContext()
    {
        using var host = BuildHostWithReadContextUnderTheBareKey();
        using var scope = host.Services.CreateScope();

        Assert.IsType<ReadProbeContext>(scope.ServiceProvider.GetRequiredService<DbContext>());

        var repository = scope.ServiceProvider.GetRequiredService<IRepository<FlushProbe, FlushProbeId>>();
        await repository.AddAsync(
            FlushProbe.Create(new FlushProbeId(Guid.NewGuid())),
            TestContext.Current.CancellationToken);

        var writeContext = scope.ServiceProvider.GetRequiredService<FlushProbeContext>();
        var readContext = scope.ServiceProvider.GetRequiredService<ReadProbeContext>();

        Assert.Single(writeContext.ChangeTracker.Entries<FlushProbeState>());
        Assert.Empty(readContext.ChangeTracker.Entries<FlushProbeState>());
    }

    [Fact]
    public void WriteDbContextAccessor_HoldsTheContextNamedByUseEfCorePersistence()
    {
        using var host = BuildHostWithReadContextUnderTheBareKey();
        using var scope = host.Services.CreateScope();

        var accessor = scope.ServiceProvider.GetRequiredService<WriteDbContextAccessor>();

        Assert.IsType<FlushProbeContext>(accessor.Context);
        Assert.Same(scope.ServiceProvider.GetRequiredService<FlushProbeContext>(), accessor.Context);
    }

    private static IHost BuildHostWithReadContextUnderTheBareKey()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddDbContext<ReadProbeContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        builder.Services.AddScoped<DbContext>(static provider => provider.GetRequiredService<ReadProbeContext>());

        builder.AddBuildingBlocks(options =>
        {
            options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
            options.UseEfCorePersistence<FlushProbeContext>(UnusedConnectionString);
        });

        return builder.Build();
    }
}

public sealed class ReadProbeContext(DbContextOptions<ReadProbeContext> options) : DbContext(options)
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
            entity.Property(state => state.Version).HasColumnName("version").IsConcurrencyToken();
        });

        modelBuilder.ApplyEntityKeyConversions();
    }
}
