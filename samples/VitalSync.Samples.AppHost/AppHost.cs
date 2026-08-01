// Deliberately separate from src/Aspire/VitalSync.AppHost: the walking skeleton is meant to be deleted
// once it has answered its questions, and the production host must not depend on it.
var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

// The write/read pair of ADR-0021. Two databases on one server, each with its own connection string;
// moving one to a dedicated server later touches no service code. The write database also holds
// Wolverine's outbox in its own "wolverine" schema, which is what keeps aggregate state and outbox
// entries inside a single transaction (ADR-0022).
var writeDb = postgres.AddDatabase("statestored-write", "statestored_write");
var readDb = postgres.AddDatabase("statestored-read", "statestored_read");

var migrations = builder.AddProject<Projects.VitalSync_Sample_StateStored_MigrationService>("statestored-migrations")
    .WithReference(writeDb)
    .WaitFor(writeDb)
    .WithReference(readDb)
    .WaitFor(readDb);

builder.AddProject<Projects.VitalSync_Sample_StateStored_Api>("statestored-api")
    .WithReference(writeDb)
    .WithReference(readDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    // Gates the API on the schema job actually finishing, so it never starts against an unmigrated
    // database. This is why the migration worker exits instead of staying alive.
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

// The event-sourced half of the skeleton. Two hosts are not optional: BuildingBlocksOptions forbids mixing
// EF Core and Marten in one host (ADR-0019/0020/0021), and each bounded context owns its own database pair.
var eventSourcedWriteDb = postgres.AddDatabase("eventsourced-write", "eventsourced_write");
var eventSourcedReadDb = postgres.AddDatabase("eventsourced-read", "eventsourced_read");

// Only the read database is migrated. Marten builds the event-store schema in the write database itself, and
// Wolverine's Marten-backed message store comes with it - so unlike the state-stored pair, the write half has
// no migration step at all.
var eventSourcedMigrations = builder
    .AddProject<Projects.VitalSync_Sample_EventSourced_MigrationService>("eventsourced-migrations")
    .WithReference(eventSourcedReadDb)
    .WaitFor(eventSourcedReadDb);

builder.AddProject<Projects.VitalSync_Sample_EventSourced_Api>("eventsourced-api")
    .WithReference(eventSourcedWriteDb)
    .WaitFor(eventSourcedWriteDb)
    .WithReference(eventSourcedReadDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(eventSourcedMigrations)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
