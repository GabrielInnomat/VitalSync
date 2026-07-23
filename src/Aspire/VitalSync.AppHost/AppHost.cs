var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume();

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume();

var nutritionDb = postgres.AddDatabase("nutritionDb", "nutritionDb");
var fitnessDb = postgres.AddDatabase("fitnessDb", "fitnessDb");

var nutritionService = builder.AddProject<Projects.VitalSync_Nutrition_Api>("nutrition-service")
    .WithReference(nutritionDb)
    .WaitFor(nutritionDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/health");

var fitnessService = builder.AddProject<Projects.VitalSync_Fitness_Api>("fitness-service")
    .WithReference(fitnessDb)
    .WaitFor(fitnessDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/health");

var backendForFrontend = builder.AddProject<Projects.VitalSync_Bff>("backend-for-frontend")
    .WithReference(nutritionService)
    .WaitFor(nutritionService)
    .WithReference(fitnessService)
    .WaitFor(fitnessService)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.VitalSync_Web>("web-frontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backendForFrontend)
    .WaitFor(backendForFrontend);

await builder.Build().RunAsync().ConfigureAwait(false);
