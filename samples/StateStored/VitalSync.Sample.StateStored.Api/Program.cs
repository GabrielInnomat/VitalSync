using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapGet("/", () => "VitalSync StateStored sample service is running.");

app.MapDefaultEndpoints();
await app.RunAsync().ConfigureAwait(false);
