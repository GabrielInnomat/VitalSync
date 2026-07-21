using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgSqlReadinessCheck(connectionName: "fitnessDb", name: "fitnessDb");
builder.AddRabbitMqReadinessCheck();

builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/", () => "VitalSync Fitness service is running.");

app.MapDefaultEndpoints();


await app.RunAsync();
