using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

using var host = builder.Build();

await host.StartAsync().ConfigureAwait(false);
await host.EnsureDatabasesExistAsync(["fitness-write", "fitness-read"]).ConfigureAwait(false);
await host.StopAsync().ConfigureAwait(false);
