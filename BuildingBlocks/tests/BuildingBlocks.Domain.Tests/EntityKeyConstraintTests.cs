using System.Reflection;
using BuildingBlocks.Domain.Entities;

namespace BuildingBlocks.Domain.Tests;

public sealed class EntityKeyConstraintTests
{
    [Fact]
    public void EveryEntityKeyTypeParameter_AlsoRequiresValueEquality()
    {
        Assert.Empty(KeyParametersWithoutValueEquality(typeof(IEntityKey).Assembly));
    }

    [Fact]
    public void TheDetector_FindsAParameterThatOnlyRequiresIEntityKey()
    {
        Assert.Equal(
            [$"{typeof(WithoutValueEquality<>).FullName}.TKey"],
            KeyParametersWithoutValueEquality(typeof(EntityKeyConstraintTests).Assembly));
    }

    internal static string[] KeyParametersWithoutValueEquality(Assembly assembly)
    {
        return [.. assembly
            .GetExportedTypes()
            .Where(type => type.IsGenericTypeDefinition)
            .SelectMany(type => type.GetGenericArguments())
            .Where(argument => argument.GetGenericParameterConstraints().Any(IsEntityKey))
            .Where(argument => !argument.GetGenericParameterConstraints().Any(IsValueEquatable))
            .Select(argument => $"{argument.DeclaringType?.FullName}.{argument.Name}")];
    }

    private static bool IsEntityKey(Type constraint)
    {
        return constraint == typeof(IEntityKey)
            || (constraint.IsGenericType && constraint.GetGenericTypeDefinition() == typeof(IEntityKey<>));
    }

    private static bool IsValueEquatable(Type constraint)
    {
        return constraint.IsGenericType && constraint.GetGenericTypeDefinition() == typeof(IEquatable<>);
    }
}

public sealed class WithoutValueEquality<TKey>
    where TKey : struct, IEntityKey
{
    public TKey Key { get; init; }
}
