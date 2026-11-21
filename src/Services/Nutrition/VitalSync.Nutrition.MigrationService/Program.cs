using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

using var host = builder.Build();

await host.StartAsync().ConfigureAwait(false);
await host.EnsureDatabasesExistAsync(["nutrition-write", "nutrition-read"]).ConfigureAwait(false);
await host.StopAsync().ConfigureAwait(false);
