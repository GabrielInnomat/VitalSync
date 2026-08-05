using BuildingBlocks.Domain.Entities;

namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal readonly record struct TestId(int Value) : IEntityKey<int>
{
    public bool IsEmpty => Value == 0;

    public static TestId Empty => new(0);

    public static TestId New(int value) => new(value);
}
