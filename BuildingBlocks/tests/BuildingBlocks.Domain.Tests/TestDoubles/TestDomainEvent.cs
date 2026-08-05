using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed record TestDomainEvent(int NewValue) : DomainEvent;

internal sealed class RawDomainEvent(int newValue) : IDomainEvent
{
    public int NewValue { get; } = newValue;
}

internal sealed record IgnoredDomainEvent : DomainEvent;
