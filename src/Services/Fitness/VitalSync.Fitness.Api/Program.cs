using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (OpenTelemetry, health checks, service discovery, resilience).
builder.AddServiceDefaults();

// Readiness checks for this service's backing infrastructure.
builder.AddNpgSqlReadinessCheck(connectionName: "fitnessDb", name: "fitnessDb");
builder.AddRabbitMqReadinessCheck();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/", () => "VitalSync Fitness service is running.");

// Maps /health and /alive per the ServiceDefaults conventions.
app.MapDefaultEndpoints();

app.Run();
