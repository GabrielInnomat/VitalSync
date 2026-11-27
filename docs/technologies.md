# Technologies

The products VitalSync is built on, and what each one is used for. The patterns these technologies
serve are described in [Patterns](./patterns.md).

## .NET and the SDK

Every project targets **.NET 10**. The SDK version is pinned in `global.json` and rolls forward
within its feature band, so every machine and every build agent compiles against the same compiler
and analyzers.

VitalSync follows **Long Term Support releases only**. Standard Term Support releases of .NET are
skipped, and a package whose newer major version requires one stays on the line that supports the
current LTS release. The same holds for the compiler: `Microsoft.CodeAnalysis.CSharp` is never newer
than the compiler the pinned SDK ships, because an analyzer referencing a newer compiler cannot be
loaded by the build.

Solution-wide settings live in `Directory.Build.props`: nullable reference types and implicit usings
are on, the analysis level is the strictest available, and warnings are errors — those of the
compiler and the analyzers as well as those that MSBuild itself and the targets of SDKs and packages
report. There is no
"we will clean up the warnings later" state — the build either is clean or it fails.

Package versions are managed centrally in `Directory.Packages.props`, so a project file references a
package by name and never carries a version of its own.

Transitive pinning is on, so a version listed there also applies wherever the package arrives
indirectly. This is why `Microsoft.CodeAnalysis.Scripting` is listed although no project references
it: Wolverine's runtime compiler requires it in an older version than the rest of Roslyn, and its
dependencies demand exactly that older compiler. Pinning it to the same version as
`Microsoft.CodeAnalysis.CSharp` keeps every Roslyn package on one version.

**Dependabot** proposes updates daily as pull requests, configured in `.github/dependabot.yml`, for
the SDK in `global.json`, the packages in `Directory.Packages.props` and the GitHub Actions. Nothing
is merged without review. Dependabot alerts and security updates are enabled for the repository as
well: a known vulnerability in a dependency raises an alert and, independently of the schedule, a
pull request to the lowest fixed version. The configuration encodes two rules:

- **LTS only.** Dependabot never proposes a new major version of the SDK, of Entity Framework Core,
  ASP.NET Core or Npgsql, because their major version follows the .NET release. Moving to the next
  LTS release is a deliberate migration that also changes the target framework. Roslyn
  (`Microsoft.CodeAnalysis*`) only receives patch updates; it is raised by hand together with the
  SDK feature band.
- **What belongs together moves together.** Packages released as one family — Aspire, Thessera,
  Entity Framework Core, Roslyn, OpenTelemetry, protobuf-net, xUnit, and the `dotnet/extensions`
  packages — arrive in one pull request per family, so they never drift apart.

Packages whose version does not follow .NET, such as `Microsoft.Extensions.Http.Resilience`, may use
a higher major version than the runtime as long as they support the current LTS target framework.

## .NET Aspire

**Aspire is the orchestrator.** It composes the whole distributed system — services, databases,
broker, frontend — and runs it locally with a single command, wiring connection strings and service
discovery between the resources.

It also owns start-up ordering, which matters here: a migration worker must finish before its
service starts, and a service must not accept traffic before its databases and the broker are
reachable.

No Aspire workload has to be installed; the AppHost is a plain .NET project that references the
Aspire hosting packages. The orchestrator and the dashboard are taken from the Aspire CLI bundle
rather than from NuGet packages. The build resolves the bundle by itself, through a compatible
`aspire` on the `PATH` or through `dnx`. The CI pipeline prepares the bundle once before it builds,
because the two AppHosts would otherwise each set it up at the same time and collide on the same
files.

The AppHost registers each project by the path of its project file, not through a project
reference, so it compiles without the projects it starts. When it runs, it builds them itself in its
own configuration before starting them; publishing and deploying never take that step.

`dotnet run` on an AppHost hands over to the Aspire CLI, which starts the Debug build whatever
configuration was requested. The CI pipeline runs a Release build, so it starts the built AppHost
assembly directly with `dotnet <AppHost>.dll`, and the AppHost builds the projects once more in
Release before starting them. The `aspire.config.json` at the repository root points `aspire run`
at `src/Aspire/VitalSync.AppHost`.

The same AppHost also describes the hosted system. A Docker Compose compute environment turns every
resource into a Compose service: each project becomes a container image built from its project file,
with no hand-written Dockerfile, and `aspire deploy` builds the images and starts the Compose
project. What differs from local development is declared in the AppHost and applies only when
publishing — restart policies, plain HTTP between the containers, a separate management port for
the health endpoints, a pgAdmin container of its own, passwords of its own rather than those of the
development run, the released dashboard image at the AppHost's Aspire version, a dashboard that takes
the scheme from the HTTPS proxy in front of it, and ports bound to the loopback address only. The
dashboard version is pinned rather than following a moving tag, so a restart never brings a
different dashboard, and it is moved up together with the Aspire packages.

The data volumes of the hosted system are declared external, under fixed names. `aspire deploy`
names the Compose project after a hash of the AppHost's path, and Compose would put that name in
front of every volume it creates, so the same system started from a checkout at another path would
silently begin on empty volumes. An external volume keeps its name whatever the project is called,
and Compose neither creates nor removes it, so the volumes are created once before the first
deployment and survive tearing the project down. Where the hosted system runs and who can reach it
is described in [Hosting](./hosting.md).

## Blazor

**Blazor is the frontend.** It is the only externally reachable endpoint of the system and holds no
business logic — it renders state and sends user intent to the BFF.

## Code-first gRPC

**gRPC carries the calls between the BFF and the services.** It is a synchronous, strongly typed and
efficient RPC mechanism, which suits an internal call between two services we own.

Contracts are defined **in C#** rather than in hand-written `.proto` files. The contract is then
ordinary code, living next to what consumes it, and it cannot drift out of sync with the types it
describes.

## PostgreSQL

**PostgreSQL is the single relational engine of the platform.** It backs both persistence
strategies: state-stored aggregates through EF Core, event-sourced aggregates through Marten. It
also hosts the write and the read database of every context. A context that mixes both strategies
keeps both stores in its one write database.

Standardizing on one engine is a deliberate simplification. It is required by Marten anyway, it
keeps the operational surface small, and it means one set of tools, one backup strategy and one
kind of connection string.

## Entity Framework Core

**EF Core is the ORM for state-stored aggregates**, running on PostgreSQL through the Npgsql
provider. It maps aggregates and their child collections to tables and provides the transaction the
unit of work commits.

Its design-time tooling belongs to the migration worker, not to the service. The migrations
themselves live next to the `DbContext` they describe.

## Marten

**Marten turns PostgreSQL into the event store** for event-sourced aggregates. It is MIT-licensed
and runs side by side with Wolverine, sharing a transaction with it.

It is used as a **raw event store**: events are appended to a stream and streams are read back,
while the folding of events into state stays in the domain. Marten's own convention-based
aggregation is deliberately not used, so the domain model keeps its shape and remains free of
persistence concerns.

Snapshotting is not used today. It can be introduced per context later without an event migration.

## RabbitMQ and Wolverine

**RabbitMQ is the message broker**, and the only channel between services.

**Wolverine is the abstraction over it**, providing publish/subscribe, the transactional outbox,
retries, dead-lettering and the durable inbox that absorbs redeliveries. It is MIT-licensed and
comes from the same ecosystem as Marten, which is why the two can share a transaction: integration
events are enqueued inside the write transaction and delivered after the commit.

Wolverine is used **as a transport only**. The in-process CQRS dispatcher stays hand-rolled and
independent of it, so the application core does not depend on a messaging framework.

## Thessera

**Thessera is the external building-block platform** VitalSync is built on, published as the
`GaWeCodes.Thessera.*` packages. It is developed in its own repository and consumed here like any
other third-party dependency.

It provides the technical foundation the services would otherwise each reinvent: the aggregate and
entity bases, strongly typed keys, the rule and result model, the CQRS dispatcher and its pipeline,
the unit of work, the outbox and publisher, the read-model rebuild machinery, and the persistence
and messaging wiring for EF Core, Marten and RabbitMQ.

Two consequences are worth knowing. Its composition entry point is called **exactly once per host**.
And because it is a separate product, its internals are documented in its own repository — this
documentation describes what VitalSync does with it, not how it works inside.

## xUnit and the test tooling

**xUnit is the test framework**, used with its built-in assertions. FluentAssertions is deliberately
not used.

**NSubstitute** provides substitutes where a collaborator has to be replaced, in application,
persistence and messaging tests. Domain tests use hand-written test doubles instead — the domain has
no infrastructure to mock.

**Testcontainers** runs a real PostgreSQL and a real RabbitMQ for integration tests, because the
behavior that matters there — optimistic concurrency, outbox flush on commit, routing, durability —
only appears against the real product.
