namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>
/// Simple <see cref="IEntityKey"/> value type used across the tests. An id is
/// considered empty when its underlying value is 0.
/// </summary>
internal readonly record struct TestId(int Value) : IEntityKey<int>
{
    public bool IsEmpty => Value == 0;

    public static TestId Empty => new(0);

    public static TestId New(int value) => new(value);
}
