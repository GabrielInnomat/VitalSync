using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
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

    [Fact]
    public void Identity_ComesFromTheDomain_NeverFromTheStore()
    {
        using var context = NewContext();

        var root = context.Model.FindEntityType(typeof(Domain.WidgetState))!;
        var rootId = root.FindProperty(nameof(Domain.WidgetState.Id))!;

        Assert.Equal(ValueGenerated.Never, rootId.ValueGenerated);
        Assert.Equal(NpgsqlValueGenerationStrategy.None, rootId.GetValueGenerationStrategy());

        var childId = root.GetNavigations().Single().TargetEntityType
            .FindProperty(nameof(Domain.WidgetPartState.Id))!;

        Assert.Equal(ValueGenerated.Never, childId.ValueGenerated);
        Assert.Equal(NpgsqlValueGenerationStrategy.None, childId.GetValueGenerationStrategy());
    }

    private static WidgetWriteDbContext NewContext() =>
        new(new DbContextOptionsBuilder<WidgetWriteDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options);
}
