var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithImageTag("4.3")
    .WithManagementPlugin();

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("18.3")
    .WithPgAdmin(pgAdmin => pgAdmin.WithImageTag("9.15.0"));

var writeDb = postgres.AddDatabase("statestored-write", "statestored_write");
var readDb = postgres.AddDatabase("statestored-read", "statestored_read");

var migrations = builder.AddDotnetProject("statestored-migrations", "../StateStored/VitalSync.Sample.StateStored.MigrationService/VitalSync.Sample.StateStored.MigrationService.csproj")
    .WithReference(writeDb)
    .WaitFor(writeDb)
    .WithReference(readDb)
    .WaitFor(readDb)
    .WithReference(messaging)
    .WaitFor(messaging);

builder.AddDotnetProject("statestored-api", "../StateStored/VitalSync.Sample.StateStored.Api/VitalSync.Sample.StateStored.Api.csproj")
    .WithReference(writeDb)
    .WithReference(readDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

var eventSourcedWriteDb = postgres.AddDatabase("eventsourced-write", "eventsourced_write");
var eventSourcedReadDb = postgres.AddDatabase("eventsourced-read", "eventsourced_read");

var eventSourcedMigrations = builder
    .AddDotnetProject("eventsourced-migrations", "../EventSourced/VitalSync.Sample.EventSourced.MigrationService/VitalSync.Sample.EventSourced.MigrationService.csproj")
    .WithReference(eventSourcedWriteDb)
    .WaitFor(eventSourcedWriteDb)
    .WithReference(eventSourcedReadDb)
    .WaitFor(eventSourcedReadDb)
    .WithReference(messaging)
    .WaitFor(messaging);

builder.AddDotnetProject("eventsourced-api", "../EventSourced/VitalSync.Sample.EventSourced.Api/VitalSync.Sample.EventSourced.Api.csproj")
    .WithReference(eventSourcedWriteDb)
    .WaitFor(eventSourcedWriteDb)
    .WithReference(eventSourcedReadDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(eventSourcedMigrations)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
