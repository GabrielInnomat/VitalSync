var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume();

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume();

var nutritionDb = postgres.AddDatabase("nutritionDb", "nutritionDb");
var fitnessDb = postgres.AddDatabase("fitnessDb", "fitnessDb");

var backendForFrontend = builder.AddProject<Projects.VitalSync_Bff>("backend-for-frontend")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.VitalSync_Web>("web-frontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backendForFrontend)
    .WaitFor(backendForFrontend);

await builder.Build().RunAsync();
