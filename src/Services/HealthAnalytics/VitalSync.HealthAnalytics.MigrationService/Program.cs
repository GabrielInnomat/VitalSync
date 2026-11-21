using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

using var host = builder.Build();

await host.StartAsync().ConfigureAwait(false);
await host.EnsureDatabasesExistAsync(["health-analytics-write", "health-analytics-read"]).ConfigureAwait(false);
await host.StopAsync().ConfigureAwait(false);
