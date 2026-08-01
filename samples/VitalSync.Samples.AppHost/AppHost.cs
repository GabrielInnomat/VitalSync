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

builder.Build().Run();
