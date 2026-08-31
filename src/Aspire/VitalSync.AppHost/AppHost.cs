var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume();

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume();

var nutritionWrite = postgres.AddDatabase("nutrition-write", "nutrition-write");
var nutritionRead = postgres.AddDatabase("nutrition-read", "nutrition-read");

var fitnessWrite = postgres.AddDatabase("fitness-write", "fitness-write");
var fitnessRead = postgres.AddDatabase("fitness-read", "fitness-read");

var healthAnalyticsWrite = postgres.AddDatabase("health-analytics-write", "health-analytics-write");
var healthAnalyticsRead = postgres.AddDatabase("health-analytics-read", "health-analytics-read");

var nutritionMigrationService = builder.AddProject<Projects.VitalSync_Nutrition_MigrationService>("nutrition-migration-service")
    .WithReference(nutritionWrite)
    .WaitFor(nutritionWrite)
    .WithReference(nutritionRead)
    .WaitFor(nutritionRead)
    .WithReference(messaging)
    .WaitFor(messaging);

var fitnessMigrationService = builder.AddProject<Projects.VitalSync_Fitness_MigrationService>("fitness-migration-service")
    .WithReference(fitnessWrite)
    .WaitFor(fitnessWrite)
    .WithReference(fitnessRead)
    .WaitFor(fitnessRead)
    .WithReference(messaging)
    .WaitFor(messaging);

var healthAnalyticsMigrationService = builder.AddProject<Projects.VitalSync_HealthAnalytics_MigrationService>("health-analytics-migration-service")
    .WithReference(healthAnalyticsWrite)
    .WaitFor(healthAnalyticsWrite)
    .WithReference(healthAnalyticsRead)
    .WaitFor(healthAnalyticsRead)
    .WithReference(messaging)
    .WaitFor(messaging);

var nutritionService = builder.AddProject<Projects.VitalSync_Nutrition_Api>("nutrition-service")
    .WithReference(nutritionRead)
    .WaitFor(nutritionRead)
    .WithReference(nutritionWrite)
    .WaitFor(nutritionWrite)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(nutritionMigrationService)
    .WithHttpHealthCheck("/health");

var fitnessService = builder.AddProject<Projects.VitalSync_Fitness_Api>("fitness-service")
    .WithReference(fitnessRead)
    .WaitFor(fitnessRead)
    .WithReference(fitnessWrite)
    .WaitFor(fitnessWrite)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(fitnessMigrationService)
    .WithHttpHealthCheck("/health");

var healthAnalyticsService = builder.AddProject<Projects.VitalSync_HealthAnalytics_Api>("health-analytics-service")
    .WithReference(healthAnalyticsRead)
    .WaitFor(healthAnalyticsRead)
    .WithReference(healthAnalyticsWrite)
    .WaitFor(healthAnalyticsWrite)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(healthAnalyticsMigrationService)
    .WithHttpHealthCheck("/health");

var backendForFrontend = builder.AddProject<Projects.VitalSync_Bff>("backend-for-frontend")
    .WithReference(nutritionService)
    .WaitFor(nutritionService)
    .WithReference(fitnessService)
    .WaitFor(fitnessService)
    .WithReference(healthAnalyticsService)
    .WaitFor(healthAnalyticsService)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.VitalSync_Web>("web-frontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backendForFrontend)
    .WaitFor(backendForFrontend);

await builder.Build().RunAsync().ConfigureAwait(false);
