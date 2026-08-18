using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Domain.Rules;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Tests;

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

    [Fact]
    public void AddPart_AppendsTheChildToTheState()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        var partId = widget.AddPart("bolt", 3);

        var part = Assert.Single(widget.Parts);
        Assert.Equal(partId, part.Id);
        Assert.Equal("bolt", part.Label);
        Assert.Equal(3, part.Quantity);

        var raised = Assert.IsType<WidgetPartAdded>(widget.DomainEvents.Last());
        Assert.Equal(widget.Id, raised.WidgetId);
        Assert.Equal(partId, raised.PartId);
    }

    [Fact]
    public void AddPart_WithAnEmptyLabelAndANonPositiveQuantity_ReportsBothFields()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        var ex = Assert.Throws<DomainValidationException>(() => widget.AddPart("  ", 0));

        Assert.Equal(2, ex.Violations.Count);
        Assert.Equal("widget.part.label.required", ex.Violations[0].Code);
        Assert.Equal("label", ex.Violations[0].Target);
        Assert.Equal("widget.part.quantity.positive", ex.Violations[1].Code);
        Assert.Equal("quantity", ex.Violations[1].Target);
    }

    [Fact]
    public void AddPart_WithOnlyTheQuantityWrong_ReportsThatFieldAlone()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        var ex = Assert.Throws<DomainValidationException>(() => widget.AddPart("bolt", 0));

        var violation = Assert.Single(ex.Violations);
        Assert.Equal("quantity", violation.Target);
    }

    [Fact]
    public void ChangePartQuantity_ReplacesOneChildAndKeepsItsIdentity()
    {
        var widget = Widget.Create(WidgetId.New(), "first");
        var first = widget.AddPart("bolt", 3);
        var second = widget.AddPart("nut", 1);

        widget.ChangePartQuantity(first, 7);

        Assert.Equal(2, widget.Parts.Count);
        Assert.Equal(7, widget.Parts.Single(part => part.Id == first).Quantity);
        Assert.Equal(1, widget.Parts.Single(part => part.Id == second).Quantity);

        var raised = Assert.IsType<WidgetPartQuantityChanged>(widget.DomainEvents.Last());
        Assert.Equal(7, raised.Quantity);
        Assert.Equal(3, raised.PreviousQuantity);
    }

    [Fact]
    public void RemovePart_DropsOnlyThatChild()
    {
        var widget = Widget.Create(WidgetId.New(), "first");
        var first = widget.AddPart("bolt", 3);
        var second = widget.AddPart("nut", 1);

        widget.RemovePart(first);

        var remaining = Assert.Single(widget.Parts);
        Assert.Equal(second, remaining.Id);

        var raised = Assert.IsType<WidgetPartRemoved>(widget.DomainEvents.Last());
        Assert.Equal(first, raised.PartId);
        Assert.Equal(3, raised.Quantity);
    }

    [Fact]
    public void PartsCollection_IsWritableSoEfCoreCanTrackIt()
    {
        var widget = Widget.Create(WidgetId.New(), "first");
        widget.AddPart("bolt", 3);

        var state = Assert.IsType<WidgetState>(((IStateOwner)widget).State);

        Assert.False(Assert.IsAssignableFrom<ICollection<WidgetPartState>>(state.Parts).IsReadOnly);
    }

    [Fact]
    public void Part_ChangesItsOwnQuantityAndTheEventLandsOnTheRoot()
    {
        var widget = Widget.Create(WidgetId.New(), "first");
        var partId = widget.AddPart("bolt", 3);

        widget.Part(partId).ChangeQuantity(7);

        Assert.Equal(7, widget.Part(partId).Quantity);
        var raised = Assert.IsType<WidgetPartQuantityChanged>(widget.DomainEvents.Last());
        Assert.Equal(widget.Id, raised.WidgetId);
        Assert.Equal(partId, raised.PartId);
        Assert.Equal(3, raised.PreviousQuantity);
        Assert.Equal(3, widget.DomainEvents.Count);
    }

    [Fact]
    public void Part_WithANonPositiveQuantity_ThrowsAndRaisesNothing()
    {
        var widget = Widget.Create(WidgetId.New(), "first");
        var partId = widget.AddPart("bolt", 3);

        Assert.Throws<DomainValidationException>(() => widget.Part(partId).ChangeQuantity(0));

        Assert.Equal(3, widget.Part(partId).Quantity);
        Assert.Equal(2, widget.DomainEvents.Count);
    }

    [Fact]
    public void Part_ThatDoesNotExist_BreaksABusinessRule()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        Assert.Throws<BusinessRuleViolationException>(() => widget.Part(WidgetPartId.New()));
    }

    [Fact]
    public void Part_ThatWasRemoved_NoLongerReadsItsState()
    {
        var widget = Widget.Create(WidgetId.New(), "first");
        var partId = widget.AddPart("bolt", 3);
        var part = widget.Part(partId);

        widget.RemovePart(partId);

        Assert.Throws<DomainValidationException>(() => part.Quantity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddPart_WithBlankLabel_ThrowsDomainValidation(string label)
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        Assert.Throws<DomainValidationException>(() => widget.AddPart(label, 1));
        Assert.Empty(widget.Parts);
    }

    [Fact]
    public void AddPart_WithNonPositiveQuantity_ThrowsDomainValidation()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        Assert.Throws<DomainValidationException>(() => widget.AddPart("bolt", 0));
    }

    [Fact]
    public void RemovePart_ThatDoesNotExist_BreaksABusinessRule()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        Assert.Throws<BusinessRuleViolationException>(() => widget.RemovePart(WidgetPartId.New()));
    }

    [Fact]
    public void ChangePartQuantity_OnAnUnknownPart_BreaksABusinessRule()
    {
        var widget = Widget.Create(WidgetId.New(), "first");

        Assert.Throws<BusinessRuleViolationException>(() => widget.ChangePartQuantity(WidgetPartId.New(), 2));
    }
}
