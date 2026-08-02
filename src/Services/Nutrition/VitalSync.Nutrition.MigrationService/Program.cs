using VitalSync.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Build();

