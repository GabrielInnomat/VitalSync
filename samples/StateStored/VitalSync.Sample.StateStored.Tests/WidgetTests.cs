using BuildingBlocks.Domain;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Tests;

// Pins the aggregate before EF Core enters the picture in the next stage: if persistence turns out to be
// broken later, these tests say whether the fold itself was ever correct.
public sealed class WidgetTests
{
    [Fact]
    public void Create_RaisesWidgetCreatedAndAdoptsTheIdentity()
    {
        var id = WidgetId.New();

        var widget = Widget.Create(id, "first");

        Assert.Equal(id, widget.Id);
        Assert.Equal("first", widget.Name);
        Assert.Equal(0, widget.RenameCount);

        var raised = Assert.IsType<WidgetCreated>(Assert.Single(widget.DomainEvents));
        Assert.Equal(id, raised.WidgetId);
        Assert.Equal("first", raised.Name);
    }

    [Fact]
    public void Rename_RaisesWidgetRenamedAndFoldsTheNewName()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        widget.Rename("second");

        Assert.Equal("second", widget.Name);
        Assert.Equal(1, widget.RenameCount);
        Assert.Equal(2, widget.DomainEvents.Count);
        Assert.IsType<WidgetRenamed>(widget.DomainEvents.Last());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainValidation(string name)
    {
        Assert.Throws<DomainValidationException>(() => Widget.Create(WidgetId.New(), name));
    }

    [Fact]
    public void Rename_WithBlankName_ThrowsAndLeavesTheAggregateUntouched()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        Assert.Throws<DomainValidationException>(() => widget.Rename(" "));

        Assert.Equal("first", widget.Name);
        Assert.Equal(0, widget.RenameCount);
        Assert.Single(widget.DomainEvents);
    }
}
