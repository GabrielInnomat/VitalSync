var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin();

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var writeDb = postgres.AddDatabase("statestored-write", "statestored_write");
var readDb = postgres.AddDatabase("statestored-read", "statestored_read");

var migrations = builder.AddProject<Projects.VitalSync_Sample_StateStored_MigrationService>("statestored-migrations")
    .WithReference(writeDb)
    .WaitFor(writeDb)
    .WithReference(readDb)
    .WaitFor(readDb)
    .WithReference(messaging)
    .WaitFor(messaging);

builder.AddProject<Projects.VitalSync_Sample_StateStored_Api>("statestored-api")
    .WithReference(writeDb)
    .WithReference(readDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

var eventSourcedWriteDb = postgres.AddDatabase("eventsourced-write", "eventsourced_write");
var eventSourcedReadDb = postgres.AddDatabase("eventsourced-read", "eventsourced_read");

var eventSourcedMigrations = builder
    .AddProject<Projects.VitalSync_Sample_EventSourced_MigrationService>("eventsourced-migrations")
    .WithReference(eventSourcedWriteDb)
    .WaitFor(eventSourcedWriteDb)
    .WithReference(eventSourcedReadDb)
    .WaitFor(eventSourcedReadDb)
    .WithReference(messaging)
    .WaitFor(messaging);

builder.AddProject<Projects.VitalSync_Sample_EventSourced_Api>("eventsourced-api")
    .WithReference(eventSourcedWriteDb)
    .WaitFor(eventSourcedWriteDb)
    .WithReference(eventSourcedReadDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(eventSourcedMigrations)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
