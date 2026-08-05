using BuildingBlocks.Domain.Entities;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed class NoOpRaiser : IDomainEventRaiser
{
    public void Raise(IDomainEvent domainEvent)
    {
    }
}

internal sealed class TestEntity(TestId id)
    : Entity<TestId, ChildState>(new NoOpRaiser(), id, _ => new ChildState(id, 0));

internal sealed class OtherTestEntity(TestId id)
    : Entity<TestId, ChildState>(new NoOpRaiser(), id, _ => new ChildState(id, 0));
