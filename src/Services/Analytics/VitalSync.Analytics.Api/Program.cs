using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgSqlReadinessCheck(connectionName: "analytics-write", name: "analytics-write");
builder.AddNpgSqlReadinessCheck(connectionName: "analytics-read", name: "analytics-read");
builder.AddRabbitMqReadinessCheck();

builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/", () => "VitalSync Analytics service is running.");

app.MapDefaultEndpoints();
await app.RunAsync().ConfigureAwait(false);
