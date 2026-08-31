using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgSqlReadinessCheck(connectionName: "health-analytics-write", name: "health-analytics-write");
builder.AddNpgSqlReadinessCheck(connectionName: "health-analytics-read", name: "health-analytics-read");
builder.AddRabbitMqReadinessCheck();

builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/", () => "VitalSync HealthAnalytics service is running.");

app.MapDefaultEndpoints();
await app.RunAsync().ConfigureAwait(false);
