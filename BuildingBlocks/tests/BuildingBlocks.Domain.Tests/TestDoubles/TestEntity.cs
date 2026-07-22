namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>Concrete entity used to exercise <see cref="Entity{TKey}"/>.</summary>
public sealed class TestEntity(TestId id) : Entity<TestId>(id);

/// <summary>
/// A second, distinct entity type sharing the same key type. Used to prove that
/// entities of different runtime types are never considered equal.
/// </summary>
public sealed class OtherTestEntity(TestId id) : Entity<TestId>(id);
