using System.Reflection;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Entities;

namespace BuildingBlocks.Application.Tests;

public sealed class EntityKeyConstraintTests
{
    [Fact]
    public void EveryEntityKeyTypeParameter_AlsoRequiresValueEquality()
    {
        var offenders = typeof(IUnitOfWork).Assembly
            .GetExportedTypes()
            .Where(type => type.IsGenericTypeDefinition)
            .SelectMany(type => type.GetGenericArguments())
            .Where(argument => argument.GetGenericParameterConstraints().Any(IsEntityKey))
            .Where(argument => !argument.GetGenericParameterConstraints().Any(IsValueEquatable))
            .Select(argument => $"{argument.DeclaringType?.FullName}.{argument.Name}")
            .ToArray();

        Assert.Empty(offenders);
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
