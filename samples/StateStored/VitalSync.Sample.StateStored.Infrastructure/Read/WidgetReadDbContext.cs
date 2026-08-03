using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Read;

public sealed class WidgetReadDbContext(DbContextOptions<WidgetReadDbContext> options) : DbContext(options)
{
    public DbSet<WidgetReadModel> Widgets => Set<WidgetReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<WidgetReadModel>(entity =>
        {
            entity.ToTable("widgets");
            entity.HasKey(widget => widget.Id);
            entity.Property(widget => widget.Id).HasColumnName("id");
            entity.Property(widget => widget.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(widget => widget.RenameCount).HasColumnName("rename_count");
        });

        modelBuilder.ApplyEntityKeyConversions();
    }
}

public sealed class WidgetReadModel
{
    public WidgetId Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RenameCount { get; set; }
}
