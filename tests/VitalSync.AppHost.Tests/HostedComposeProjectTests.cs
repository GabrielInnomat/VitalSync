using System.Text.RegularExpressions;
using static VitalSync.AppHost.Tests.ComposeNode;

namespace VitalSync.AppHost.Tests;

public class HostedComposeProjectTests(PublishedComposeProject compose) : IClassFixture<PublishedComposeProject>
{
    private static readonly string[] HostedProjects =
        ["nutrition-service", "fitness-service", "health-analytics-service", "backend-for-frontend", "web-frontend"];

    private static readonly string[] MigrationWorkers =
        ["nutrition-migration-service", "fitness-migration-service", "health-analytics-migration-service"];

    public static TheoryData<string> LongRunningServices =>
        [.. HostedProjects, "messaging", "postgres", "pgadmin"];

    public static TheoryData<string> HostedProjectNames => [.. HostedProjects];

    public static TheoryData<string> MigrationWorkerNames => [.. MigrationWorkers];

    [Theory]
    [MemberData(nameof(LongRunningServices))]
    public void LongRunningService_RestartsUnlessStopped(string name)
    {
        Assert.Equal("unless-stopped", Scalar(compose.Service(name), "restart"));
    }

    [Theory]
    [MemberData(nameof(MigrationWorkerNames))]
    public void MigrationWorker_IsNotRestarted(string name)
    {
        Assert.Null(Scalar(compose.Service(name), "restart"));
    }

    [Fact]
    public void PublishedPorts_AreExactlyTheIntendedOnesAndBoundToLoopback()
    {
        var published = compose.Services
            .SelectMany(service => Sequence(service.Value, "ports").Select(port => $"{service.Key} {port}"))
            .Order(StringComparer.Ordinal);

        Assert.Equal(
        [
            "messaging 127.0.0.1:15672:15672",
            "pgadmin 127.0.0.1:5050:80",
            "vitalsync-dashboard 127.0.0.1:18888:18888",
            "web-frontend 127.0.0.1:8080:8080",
        ],
            published);
    }

    [Theory]
    [MemberData(nameof(HostedProjectNames))]
    public void HostedProject_ListensOnTheApplicationAndTheManagementPort(string name)
    {
        var environment = Section(compose.Service(name), "environment");

        Assert.Equal("8080;8081", Scalar(environment, "HTTP_PORTS"));
        Assert.Equal("8081", Scalar(environment, "ManagementPort"));
    }

    [Theory]
    [MemberData(nameof(HostedProjectNames))]
    public void HostedProject_UsesPlainHttpOnly(string name)
    {
        var environment = Section(compose.Service(name), "environment");

        Assert.Equal("true", Scalar(environment, "ServiceDiscovery__AllowPlainHttp"));
        Assert.DoesNotContain(environment.Values.OfType<string>(), value => value.StartsWith("https://", StringComparison.Ordinal));
    }

    [Fact]
    public void Volumes_HaveStableNames()
    {
        Assert.Equal(
            ["vitalsync-messaging-data", "vitalsync-pgadmin-data", "vitalsync-postgres-data", "vitalsync-web-keys"],
            compose.VolumeNames.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Volumes_AreExternalSoTheirNamesDoNotDependOnTheProjectName()
    {
        Assert.All(compose.Volumes, volume =>
        {
            Assert.Equal("true", Scalar(volume.Value, "external"));
            Assert.Null(Scalar(volume.Value, "driver"));
        });
    }

    [Fact]
    public void VolumeSetupScript_CreatesExactlyThePublishedVolumes()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "tools", "hosting", "New-VitalSyncVolumes.ps1"));

        var created = Regex.Matches(script, "'(vitalsync-[a-z-]+)'")
            .Select(match => match.Groups[1].Value)
            .Order(StringComparer.Ordinal);

        Assert.Equal(compose.VolumeNames.Order(StringComparer.Ordinal), created);
    }

    [Fact]
    public void WebFrontend_KeepsItsHomeDirectoryOnAVolume()
    {
        var volume = Assert.Single(Mappings(compose.Service("web-frontend"), "volumes"));

        Assert.Equal("vitalsync-web-keys", Scalar(volume, "source"));
        Assert.Equal("/home/app", Scalar(volume, "target"));
    }

    [Fact]
    public void PgAdmin_RequiresALoginFromParameters()
    {
        var environment = Section(compose.Service("pgadmin"), "environment");

        Assert.Equal("${PGADMIN_EMAIL}", Scalar(environment, "PGADMIN_DEFAULT_EMAIL"));
        Assert.Equal("${PGADMIN_PASSWORD}", Scalar(environment, "PGADMIN_DEFAULT_PASSWORD"));
        Assert.False(environment.ContainsKey("PGADMIN_CONFIG_SERVER_MODE"));
    }

    [Fact]
    public void Passwords_ComeFromHostedParametersAndAreNotWrittenIntoTheComposeFile()
    {
        Assert.Equal("${HOSTED_POSTGRES_PASSWORD}", Scalar(Section(compose.Service("postgres"), "environment"), "POSTGRES_PASSWORD"));
        Assert.Equal("${HOSTED_MESSAGING_PASSWORD}", Scalar(Section(compose.Service("messaging"), "environment"), "RABBITMQ_DEFAULT_PASS"));
    }

    [Fact]
    public void Dashboard_RunsTheReleasedImageAtAFixedVersion()
    {
        Assert.Equal("mcr.microsoft.com/aspire/dashboard:13.6.5", Scalar(compose.Service("vitalsync-dashboard"), "image"));
    }

    [Fact]
    public void Dashboard_TakesTheSchemeFromTheProxyInFrontOfIt()
    {
        var environment = Section(compose.Service("vitalsync-dashboard"), "environment");

        Assert.Equal("true", Scalar(environment, "ASPNETCORE_FORWARDEDHEADERS_ENABLED"));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VitalSync.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("The repository root with VitalSync.slnx was not found.");
    }
}
