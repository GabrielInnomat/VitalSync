using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
