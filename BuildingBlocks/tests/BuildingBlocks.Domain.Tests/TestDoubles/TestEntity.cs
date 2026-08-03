namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed class TestEntity(TestId id) : Entity<TestId>(id);

internal sealed class OtherTestEntity(TestId id) : Entity<TestId>(id);
