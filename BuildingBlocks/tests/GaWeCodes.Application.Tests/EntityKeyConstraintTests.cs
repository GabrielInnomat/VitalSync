using System.Reflection;
using GaWeCodes.Application.Persistence;
using GaWeCodes.Domain.Entities;

namespace GaWeCodes.Application.Tests;

public sealed class EntityKeyConstraintTests
{
    [Fact]
    public void EveryEntityKeyTypeParameter_AlsoRequiresValueEquality()
    {
        Assert.Empty(KeyParametersWithoutValueEquality(typeof(IUnitOfWork).Assembly));
    }

    [Fact]
    public void TheDetector_FindsAParameterThatOnlyRequiresIEntityKey()
    {
        Assert.Equal(
            [$"{typeof(WithoutValueEqualityOnTheType<>).FullName}.TKey"],
            KeyParametersWithoutValueEquality(typeof(EntityKeyConstraintTests).Assembly)
                .Where(offender => offender.Contains("OnTheType", StringComparison.Ordinal))
                .ToArray());
    }

    [Fact]
    public void TheDetector_AlsoLooksAtGenericMethods()
    {
        Assert.Contains(
            $"{typeof(WithoutValueEqualityOnAMethod).FullName}.{nameof(WithoutValueEqualityOnAMethod.Take)}.TKey",
            KeyParametersWithoutValueEquality(typeof(EntityKeyConstraintTests).Assembly));
    }

    internal static string[] KeyParametersWithoutValueEquality(Assembly assembly)
    {
        var exported = assembly.GetExportedTypes();

        var fromTypes = exported
            .Where(type => type.IsGenericTypeDefinition)
            .SelectMany(type => type.GetGenericArguments())
            .Select(argument => (Argument: argument, Name: $"{argument.DeclaringType?.FullName}.{argument.Name}"));

        var fromMethods = exported
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(method => method.IsGenericMethodDefinition)
            .SelectMany(method => method.GetGenericArguments()
                .Select(argument => (
                    Argument: argument,
                    Name: $"{method.DeclaringType?.FullName}.{method.Name}.{argument.Name}")));

        return
        [
            .. fromTypes.Concat(fromMethods)
                .Where(candidate => candidate.Argument.GetGenericParameterConstraints().Any(IsEntityKey))
                .Where(candidate => !candidate.Argument.GetGenericParameterConstraints().Any(IsValueEquatable))
                .Select(candidate => candidate.Name),
        ];
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

public sealed class WithoutValueEqualityOnTheType<TKey>
    where TKey : struct, IEntityKey
{
    public TKey Key { get; init; }
}

public sealed class WithoutValueEqualityOnAMethod
{
    public static TKey Take<TKey>(TKey key)
        where TKey : struct, IEntityKey => key;
}
