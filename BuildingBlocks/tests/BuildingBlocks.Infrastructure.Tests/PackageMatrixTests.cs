using System.Text.Json;
using BuildingBlocks.Application.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class PackageMatrixTests
{
    private const string Marten = "Marten";

    private const string EfCore = "Microsoft.EntityFrameworkCore";

    private const string RabbitMq = "RabbitMQ.Client";

    private const string Wolverine = "WolverineFx";

    private const string Npgsql = "Npgsql";

    private static readonly string[] AllVendors = [Marten, EfCore, RabbitMq, Wolverine, Npgsql];

    public static TheoryData<string, string[]> ForbiddenVendorsPerHost =>
        new()
        {
            { "BareHost", [Marten, EfCore, RabbitMq, Wolverine, Npgsql] },
            { "StateHost", [Marten, RabbitMq] },
            { "EventsHost", [EfCore, RabbitMq] },
            { "StateBusHost", [Marten] },
            { "EventsBusHost", [EfCore] },
        };

    public static TheoryData<string, string[]> RequiredVendorsPerHost =>
        new()
        {
            { "StateHost", [EfCore, Npgsql, Wolverine] },
            { "EventsHost", [Marten, Npgsql, Wolverine] },
            { "StateBusHost", [EfCore, RabbitMq, Wolverine] },
            { "EventsBusHost", [Marten, RabbitMq, Wolverine] },
        };

    public static TheoryData<string> HostNames =>
        ["BareHost", "StateHost", "EventsHost", "StateBusHost", "EventsBusHost"];

    [Theory]
    [MemberData(nameof(ForbiddenVendorsPerHost))]
    public void AHostNeverRestoresAVendorItDidNotChoose(string host, string[] forbidden)
    {
        var restored = RestoredPackages(host);

        var leaked = forbidden
            .Where(vendor => restored.Any(package => package.StartsWith(vendor, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(leaked);
    }

    [Theory]
    [MemberData(nameof(RequiredVendorsPerHost))]
    public void AHostRestoresEveryVendorItDidChoose(string host, string[] required)
    {
        var restored = RestoredPackages(host);

        var missing = required
            .Where(vendor => !restored.Any(package => package.StartsWith(vendor, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void TheDetectorTellsAPresentVendorFromAnAbsentOne()
    {
        var withEverything = VendorsIn("EventsBusHost");
        var withNothing = VendorsIn("BareHost");

        Assert.Equal([Marten, RabbitMq, Wolverine, Npgsql], withEverything);
        Assert.Empty(withNothing);
    }

    [Theory]
    [MemberData(nameof(HostNames))]
    public void EveryPackageCombinationWiresItselfUp(string host)
    {
        using var built = BuildHost(host);

        Assert.NotNull(built.Services.GetRequiredService<ISender>());
    }

    private static IHost BuildHost(string host) => host switch
    {
        "BareHost" => BareHost.MatrixHost.Build(),
        "StateHost" => StateHost.MatrixHost.Build(),
        "EventsHost" => EventsHost.MatrixHost.Build(),
        "StateBusHost" => StateBusHost.MatrixHost.Build(),
        "EventsBusHost" => EventsBusHost.MatrixHost.Build(),
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown matrix host."),
    };

    private static string[] VendorsIn(string host)
    {
        var restored = RestoredPackages(host);

        return
        [
            .. AllVendors.Where(vendor =>
                restored.Any(package => package.StartsWith(vendor, StringComparison.Ordinal))),
        ];
    }

    private static string[] RestoredPackages(string host)
    {
        var assets = Path.Combine(
            BuildingBlocksRoot(),
            "tests",
            "MatrixHosts",
            host,
            "obj",
            "project.assets.json");

        Assert.True(File.Exists(assets), $"'{assets}' does not exist; '{host}' has not been restored.");

        using var document = JsonDocument.Parse(File.ReadAllText(assets));

        return
        [
            .. document.RootElement
                .GetProperty("libraries")
                .EnumerateObject()
                .Select(library => library.Name),
        ];
    }

    private static string BuildingBlocksRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.EnumerateFiles(directory.FullName, "*.slnx").Any())
        {
            directory = directory.Parent;
        }

        Assert.True(
            directory is not null,
            "No directory containing a '*.slnx' file was found above "
            + $"'{AppContext.BaseDirectory}'; the Building Blocks root cannot be located.");

        return directory!.FullName;
    }
}
