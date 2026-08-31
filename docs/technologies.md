# Technologies

The products VitalSync is built on, and what each one is used for. The patterns these technologies
serve are described in [Patterns](./patterns.md).

## .NET and the SDK

Every project targets **.NET 10**. The SDK version is pinned in `global.json` and rolls forward
within its feature band, so every machine and every build agent compiles against the same compiler
and analyzers.

Solution-wide settings live in `Directory.Build.props`: nullable reference types and implicit usings
are on, the analysis level is the strictest available, and warnings are errors. There is no
"we will clean up the warnings later" state — the build either is clean or it fails.

Package versions are managed centrally in `Directory.Packages.props`, so a project file references a
package by name and never carries a version of its own.

## .NET Aspire

**Aspire is the orchestrator.** It composes the whole distributed system — services, databases,
broker, frontend — and runs it locally with a single command, wiring connection strings and service
discovery between the resources.

It also owns start-up ordering, which matters here: a migration worker must finish before its
service starts, and a service must not accept traffic before its databases and the broker are
reachable.

No Aspire workload has to be installed; the AppHost references the Aspire SDK as a package.

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
strategies: state-stored contexts through EF Core, event-sourced contexts through Marten. It also
hosts the write and the read database of every context.

Standardizing on one engine is a deliberate simplification. It is required by Marten anyway, it
keeps the operational surface small, and it means one set of tools, one backup strategy and one
kind of connection string.

## Entity Framework Core

**EF Core is the ORM for state-stored contexts**, running on PostgreSQL through the Npgsql provider.
It maps aggregates and their child collections to tables and provides the transaction the unit of
work commits.

Its design-time tooling belongs to the migration worker, not to the service. The migrations
themselves live next to the `DbContext` they describe.

## Marten

**Marten turns PostgreSQL into the event store** for event-sourced contexts. It is MIT-licensed and
runs side by side with Wolverine, sharing a transaction with it.

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
