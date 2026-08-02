using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgSqlReadinessCheck(connectionName: "fitness-write", name: "fitness-write");
builder.AddNpgSqlReadinessCheck(connectionName: "fitness-read", name: "fitness-read");
builder.AddRabbitMqReadinessCheck();

builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/", () => "VitalSync Fitness service is running.");

app.MapDefaultEndpoints();
await app.RunAsync().ConfigureAwait(false);
