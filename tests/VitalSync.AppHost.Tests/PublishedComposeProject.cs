using Aspire.Hosting.Testing;
using YamlDotNet.Serialization;

namespace VitalSync.AppHost.Tests;

public sealed class PublishedComposeProject : IAsyncLifetime
{
    private readonly DirectoryInfo outputDirectory = Directory.CreateTempSubdirectory("vitalsync-publish-");

    public IReadOnlyDictionary<string, Dictionary<object, object>> Services { get; private set; } =
        new Dictionary<string, Dictionary<object, object>>();

    public IReadOnlyDictionary<string, Dictionary<object, object>> Volumes { get; private set; } =
        new Dictionary<string, Dictionary<object, object>>();

    public IReadOnlyCollection<string> VolumeNames => [.. Volumes.Keys];

    public async ValueTask InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.VitalSync_AppHost>(
            ["--operation", "publish", "--step", "publish", "--output-path", outputDirectory.FullName],
            timeout.Token);

        await using var app = await builder.BuildAsync(timeout.Token);
        await app.RunAsync(timeout.Token);

        var yaml = await File.ReadAllTextAsync(Path.Combine(outputDirectory.FullName, "docker-compose.yaml"), timeout.Token);
        var document = new DeserializerBuilder().Build().Deserialize<Dictionary<object, object>>(yaml);

        Services = ComposeNode.Section(document, "services")
            .ToDictionary(service => (string)service.Key, service => (Dictionary<object, object>)service.Value);
        Volumes = ComposeNode.Section(document, "volumes")
            .ToDictionary(volume => (string)volume.Key, volume => volume.Value as Dictionary<object, object> ?? []);
    }

    public ValueTask DisposeAsync()
    {
        outputDirectory.Delete(recursive: true);
        return ValueTask.CompletedTask;
    }

    public Dictionary<object, object> Service(string name) =>
        Services.TryGetValue(name, out var service)
            ? service
            : throw new KeyNotFoundException($"The published Compose project has no service named '{name}'.");
}
