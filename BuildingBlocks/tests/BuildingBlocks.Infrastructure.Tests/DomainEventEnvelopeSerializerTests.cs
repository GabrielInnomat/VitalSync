using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class DomainEventEnvelopeSerializerTests
{
    [Fact]
    public void WrapThenUnwrap_WithTypedIdDecimalAndDateTimeOffset_RoundTripsAllValues()
    {
        var original = new RecipeRenamed(
            new RecipeId(Guid.NewGuid()),
            NewName: "Pasta",
            Rating: 4.75m,
            RenamedAt: new DateTimeOffset(2026, 7, 30, 9, 30, 0, TimeSpan.FromHours(2)));

        var restored = (RecipeRenamed)DomainEventEnvelopeSerializer.Unwrap(
            DomainEventEnvelopeSerializer.Wrap(original));

        Assert.Equal(original.EventId, restored.EventId);
        Assert.Equal(original.RecipeId, restored.RecipeId);
        Assert.Equal(original.NewName, restored.NewName);
        Assert.Equal(original.Rating, restored.Rating);
        Assert.Equal(original.RenamedAt, restored.RenamedAt);
    }

    [Fact]
    public void Wrap_CarriesTheAssemblyQualifiedEventTypeName()
    {
        var envelope = DomainEventEnvelopeSerializer.Wrap(
            new RecipeRenamed(new RecipeId(Guid.NewGuid()), "Pizza", 5m, DateTimeOffset.UnixEpoch));

        Assert.Equal(typeof(RecipeRenamed).AssemblyQualifiedName, envelope.EventTypeName);
    }

    [Fact]
    public void Unwrap_WithUnknownTypeName_ThrowsAClearException_NotNullReference()
    {
        var envelope = new DomainEventEnvelope("BuildingBlocks.Nonexistent.Event, Nonexistent.Assembly", "{}");

        var exception = Record.Exception(() => DomainEventEnvelopeSerializer.Unwrap(envelope));

        Assert.NotNull(exception);
        Assert.IsNotType<NullReferenceException>(exception);
    }

    private sealed record RecipeRenamed(RecipeId RecipeId, string NewName, decimal Rating, DateTimeOffset RenamedAt)
        : DomainEvent;

    private readonly record struct RecipeId(Guid Value) : IEntityKey<Guid>
    {
        public bool IsEmpty => Value == Guid.Empty;
    }
}
