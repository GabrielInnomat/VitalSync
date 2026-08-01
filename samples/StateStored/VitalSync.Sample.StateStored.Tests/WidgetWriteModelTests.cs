using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Infrastructure.Write;

namespace VitalSync.Sample.StateStored.Tests;

// Model building and validation happen without touching a database, so this is the cheapest possible
// answer to "can EF Core map an aggregate whose Id is computed from its state object?".
public sealed class WidgetWriteModelTests
{
    [Fact]
    public void WriteModel_BuildsAndValidates()
    {
        var options = new DbContextOptionsBuilder<WidgetWriteDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;

        using var context = new WidgetWriteDbContext(options);

        // The state is the mapped entity type, not the aggregate.
        var entityType = context.Model.FindEntityType(typeof(Domain.WidgetState));

        Assert.NotNull(entityType);
        Assert.Equal("widgets", entityType!.GetTableName());
        Assert.Null(context.Model.FindEntityType(typeof(Domain.Widget)));

        // One identity column, mapped straight from the state's own key - no shadow key, no duplication.
        var key = Assert.Single(entityType.GetKeys());
        var keyProperty = Assert.Single(key.Properties);
        Assert.Equal("id", keyProperty.GetColumnName());
    }
}
