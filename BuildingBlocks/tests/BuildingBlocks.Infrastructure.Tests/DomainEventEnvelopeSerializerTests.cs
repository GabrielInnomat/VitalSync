using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class DomainEventEnvelopeSerializerTests
{
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly DateTimeOffset OccurredAt = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WrapThenUnwrap_WithTypedIdDecimalAndDateTimeOffset_RoundTripsAllValues()
    {
        var original = new RecipeRenamed(
            new RecipeId(Guid.NewGuid()),
            NewName: "Pasta",
            Rating: 4.75m,
            RenamedAt: new DateTimeOffset(2026, 7, 30, 9, 30, 0, TimeSpan.FromHours(2)));

        var restored = (RecipeRenamed)DomainEventEnvelopeSerializer.Unwrap(
            DomainEventEnvelopeSerializer.Wrap(original, EventId, OccurredAt));

        Assert.Equal(original, restored);
    }

    [Fact]
    public void Wrap_CarriesTheAssemblyQualifiedEventTypeName()
    {
        var envelope = DomainEventEnvelopeSerializer.Wrap(
            new RecipeRenamed(new RecipeId(Guid.NewGuid()), "Pizza", 5m, DateTimeOffset.UnixEpoch), EventId, OccurredAt);

        Assert.Equal(typeof(RecipeRenamed).AssemblyQualifiedName, envelope.EventTypeName);
    }

    [Fact]
    public void Wrap_CarriesEventIdAndOccurredAtOnTheEnvelope()
    {
        var envelope = DomainEventEnvelopeSerializer.Wrap(
            new RecipeRenamed(new RecipeId(Guid.NewGuid()), "Pizza", 5m, DateTimeOffset.UnixEpoch), EventId, OccurredAt);

        Assert.Equal(EventId, envelope.EventId);
        Assert.Equal(OccurredAt, envelope.OccurredAt);
    }

    [Fact]
    public void Unwrap_WithUnknownTypeName_ThrowsAClearException_NotNullReference()
    {
        var envelope = new DomainEventEnvelope("BuildingBlocks.Nonexistent.Event, Nonexistent.Assembly", "{}", EventId, OccurredAt);

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
