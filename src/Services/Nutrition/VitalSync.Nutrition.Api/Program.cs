using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgSqlReadinessCheck(connectionName: "nutrition-write", name: "nutrition-write");
builder.AddNpgSqlReadinessCheck(connectionName: "nutrition-read", name: "nutrition-read");
builder.AddRabbitMqReadinessCheck();

builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/", () => "VitalSync Nutrition service is running.");

app.MapDefaultEndpoints();
await app.RunAsync().ConfigureAwait(false);
