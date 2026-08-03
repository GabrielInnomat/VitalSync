using Microsoft.EntityFrameworkCore;
using VitalSync.Sample.StateStored.Infrastructure.Write;

namespace VitalSync.Sample.StateStored.Tests;

public sealed class WidgetWriteModelTests
{
    [Fact]
    public void WriteModel_BuildsAndValidates()
    {
        var options = new DbContextOptionsBuilder<WidgetWriteDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;

        using var context = new WidgetWriteDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Domain.WidgetState));

        Assert.NotNull(entityType);
        Assert.Equal("widgets", entityType!.GetTableName());
        Assert.Null(context.Model.FindEntityType(typeof(Domain.Widget)));

        var key = Assert.Single(entityType.GetKeys());
        var keyProperty = Assert.Single(key.Properties);
        Assert.Equal("id", keyProperty.GetColumnName());
    }
}
