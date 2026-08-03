using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Infrastructure.Read;

public sealed class GadgetReadDbContext(DbContextOptions<GadgetReadDbContext> options) : DbContext(options)
{
    public DbSet<GadgetReadModel> Gadgets => Set<GadgetReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<GadgetReadModel>(entity =>
        {
            entity.ToTable("gadgets");
            entity.HasKey(gadget => gadget.Id);
            entity.Property(gadget => gadget.Id).HasColumnName("id");
            entity.Property(gadget => gadget.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(gadget => gadget.RenameCount).HasColumnName("rename_count");
            entity.Property(gadget => gadget.IsRetired).HasColumnName("is_retired");
        });

        modelBuilder.ApplyEntityKeyConversions();
    }
}

public sealed class GadgetReadModel
{
    public GadgetId Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RenameCount { get; set; }

    public bool IsRetired { get; set; }
}
