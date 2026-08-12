using System.Reflection;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Persistence.StateStored;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class CoreFacadeVendorNeutralityTests
{
    private static readonly string[] VendorAssemblies =
    [
        "Marten",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "RabbitMQ.Client",
        "Wolverine",
    ];

    public static TheoryData<Type> FacadeTypes =>
    [
        typeof(BuildingBlocksOptions),
        typeof(ServiceCollectionExtensions),
    ];

    [Theory]
    [MemberData(nameof(FacadeTypes))]
    public void NoFacadeSignatureNamesAVendorType(Type facade)
    {
        ArgumentNullException.ThrowIfNull(facade);

        Assert.Empty(FindVendorTypes(facade));
    }

    [Fact]
    public void TheHostBuilderEntryPointStillNamesWolverine()
    {
        Assert.NotEmpty(FindVendorTypes(typeof(HostApplicationBuilderExtensions)));
    }

    [Fact]
    public void TheDetectorRecognisesAVendorTypeWhereOneIsExpected()
    {
        Assert.NotEmpty(FindVendorTypes(typeof(EfCorePersistenceOptionsExtensions)));
    }

    private static string[] FindVendorTypes(Type declaringType) =>
        declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .SelectMany(method => method
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .Append(method.ReturnType)
                .SelectMany(Unwrap)
                .Where(IsVendorType)
                .Select(type => $"{declaringType.Name}.{method.Name} names '{type.Name}'"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<Type> Unwrap(Type type)
    {
        yield return type;

        foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
        {
            foreach (var nested in Unwrap(argument))
            {
                yield return nested;
            }
        }
    }

    private static bool IsVendorType(Type type)
    {
        var assembly = type.Assembly.GetName().Name;

        return assembly is not null
            && Array.Exists(
                VendorAssemblies,
                vendor => assembly.Equals(vendor, StringComparison.Ordinal)
                    || assembly.StartsWith($"{vendor}.", StringComparison.Ordinal));
    }
}
