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

    [Fact]
    public void ChildCollection_MapsAsAnOwnedTableWithItsOwnDomainKey()
    {
        using var context = NewContext();

        var entityType = context.Model.FindEntityType(typeof(Domain.WidgetState))!;
        var navigation = Assert.Single(entityType.GetNavigations());

        Assert.Equal(nameof(Domain.WidgetState.Parts), navigation.Name);
        Assert.True(navigation.IsCollection);
        Assert.True(navigation.ForeignKey.IsOwnership);

        var parts = navigation.TargetEntityType;
        Assert.Equal("widget_parts", parts.GetTableName());

        var key = Assert.Single(parts.GetKeys());
        var keyProperty = Assert.Single(key.Properties);
        Assert.Equal("id", keyProperty.GetColumnName());
        Assert.Equal(typeof(Domain.WidgetPartId), keyProperty.ClrType);
        Assert.NotNull(keyProperty.GetValueConverter());

        var foreignKeyProperty = Assert.Single(navigation.ForeignKey.Properties);
        Assert.Equal("widget_id", foreignKeyProperty.GetColumnName());
    }

    [Fact]
    public void RootVersion_StaysTheConcurrencyToken()
    {
        using var context = NewContext();

        var version = context.Model.FindEntityType(typeof(Domain.WidgetState))!
            .FindProperty(nameof(Domain.WidgetState.Version))!;

        Assert.True(version.IsConcurrencyToken);
    }

    private static WidgetWriteDbContext NewContext() =>
        new(new DbContextOptionsBuilder<WidgetWriteDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options);
}
