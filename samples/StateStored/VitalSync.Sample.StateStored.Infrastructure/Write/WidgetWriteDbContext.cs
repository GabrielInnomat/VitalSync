using GaWeCodes.Domain.Entities;
using GaWeCodes.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Write;

public sealed class WidgetWriteDbContext(DbContextOptions<WidgetWriteDbContext> options) : DbContext(options)
{
    public DbSet<WidgetState> Widgets => Set<WidgetState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<WidgetState>(entity =>
        {
            entity.ToTable("widgets");
            entity.HasKey(state => state.Id);
            entity.Property(state => state.Id).HasColumnName("id");
            entity.Property(state => state.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(state => state.RenameCount).HasColumnName("rename_count");
            entity.Property(state => state.Version).HasColumnName("version").IsConcurrencyToken();

            entity.OwnsMany(state => state.Parts, parts =>
            {
                parts.ToTable("widget_parts");
                parts.WithOwner().HasForeignKey("widget_id");
                parts.HasKey(part => part.Id);
                parts.Property(part => part.Id).HasColumnName("id");
                parts.Property(part => part.Label).HasColumnName("label").IsRequired().HasMaxLength(200);
                parts.Property(part => part.Quantity).HasColumnName("quantity");
            });
        });

        modelBuilder.ApplyEntityKeyConversions();
    }
}
