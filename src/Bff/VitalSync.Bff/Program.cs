using VitalSync.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.MapControllers();
await app.RunAsync().ConfigureAwait(false);
