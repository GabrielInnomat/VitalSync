# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What VitalSync is

A cloud-native, distributed platform unifying **nutrition**, **fitness**, and **health analytics** behind a single Blazor UI. Built as independent ASP.NET Core microservices using **DDD**, **CQRS**, and **selective Event Sourcing**.

Core principle: Technical/architectural decisions are mandatory and stable. Business/domain details are refined iteratively as the project evolves (it is early-stage: `src/Services/*` currently contain only placeholder API projects). When a change affects architecture, add or superseed an ADR.

## Build, test, run

```bash
dotnet build                                        # build the solution (VitalSync.slnx)
dotnet test                                         # run all tests
dotnet test --filter "FullyQualifiedName~AggregateRootTests"   # run a single test class
dotnet test BuildingBlocks/tests/BuildingBlocks.Domain.Tests   # run one test project
dotnet run --project src/Aspire/VitalSync.AppHost   # run the full system via Aspire
```

Prerequisites: .NET SDK aligned with .NET Aspire 13, the Aspire 13 workload, and Docker (for messaging/database infrastructure).

Global build settings (`Directory.Build.props`) apply solution-wide: nullable + implicit usings enabled, `latest-all` analysis level, **warnings treated as errors**, and `GenerateDocumentationFile` on. Respect `.editorconfig` at each level (root, `src/`, `tests/`, and a stricter one under `BuildingBlocks/src/*` — see XML docs below).

## Repository map

```text
VitalSync/
├── BuildingBlocks/                 # Reusable, VitalSync-INDEPENDENT platform
│   ├── src/
│   │   ├── BuildingBlocks.Domain/          # Aggregates, entities, domain events, typed IDs, business rules
│   │   ├── BuildingBlocks.Application/     # CQRS abstractions (commands/queries/handlers), Result/Failure
│   │   └── BuildingBlocks.Infrastructure/  # DI-based dispatcher, event sourcing (Marten), EF Core persistence,
│   │                                       # outbox, projections, Wolverine/RabbitMQ transport
│   └── tests/                      # Mirrors src/ with *.Tests projects (Domain.Tests, Application.Tests)
├── src/                             # VitalSync APPLICATION
│   ├── Aspire/                      # .NET Aspire AppHost & ServiceDefaults (entry point)
│   ├── Bff/                         # Backend-for-Frontend (REST out, gRPC in)
│   ├── Frontend/VitalSync.Web/      # Blazor client (UI only — no business logic)
│   └── Services/                    # One folder per microservice (currently placeholder Api projects)
│       ├── Nutrition/VitalSync.Nutrition.Api/
│       └── Fitness/VitalSync.Fitness.Api/
├── docs/architecture/               # Architecture docs, ADRs (decisions/), glossary, user stories
└── tests/VitalSync.Tests/           # Cross-cutting / integration tests
```

Guidance for finding things:

- **Shared/reusable concepts** (base aggregate, domain event, typed IDs, CQRS interfaces, `Result`) → `BuildingBlocks/src/...`. Must stay framework-agnostic and independent of VitalSync itself.
- **Business logic** → the relevant service under `src/Services/<Domain>/`.
- **UI** → `src/Frontend/`.
- **Entry point / running the system** → `src/Aspire/`.

## Architecture & communication rules (do not violate)

- The Blazor frontend communicates **exclusively** through the **BFF**.
- The BFF exposes **REST** to the frontend and talks to microservices via **code-first gRPC**.
- Microservices **never** call each other synchronously. All inter-service communication is **asynchronous**, via RabbitMQ/Wolverine.
- Layer separation (Domain / Application / Infrastructure / Persistence) is mandatory; dependencies point inward — the domain has no infrastructure dependencies.

See `docs/architecture/communication.md`.

## Technology stack

| Concern                 | Choice                                                                                                                                   |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| Orchestration           | .NET Aspire 13                                                                                                                           |
| Frontend                | Blazor (UI only)                                                                                                                         |
| Backend-for-Frontend    | REST (to frontend) + code-first gRPC (to services)                                                                                       |
| Microservices           | ASP.NET Core, one per business area                                                                                                      |
| Inter-service messaging | RabbitMQ via Wolverine (ADR-0023, supersedes ADR-0004); transport-only, not the CQRS mediator                                            |
| Persistence             | EF Core on PostgreSQL by default; Marten (event sourcing) on PostgreSQL where ES adds value (ADR-0019/0020)                              |
| Database topology       | PostgreSQL; a write + read database pair per bounded context (ADR-0021); shared server now, server-per-context possible later (ADR-0020) |
| Read models             | Event-driven projections in each context's read DB via an outbox-backed publisher (ADR-0022)                                             |
| Patterns                | DDD, CQRS, Event Sourcing (selective)                                                                                                    |
| Testing                 | xUnit (incl. built-in asserts), NSubstitute, EF Core InMemory — **no FluentAssertions** (ADR-0014)                                       |

## Domain / DDD conventions (from accepted ADRs)

- Strongly typed aggregate identifiers (ADR-0005) — no raw `Guid`/`int` IDs.
- The aggregate owns its domain events (ADR-0006); read-only vs. managed exposure per ADR-0007.
- Entity identity and equality follow ADR-0008; business rules/domain validation follow ADR-0009.
- Aggregates use an aggregate state object (ADR-0010).
- Event sourcing is optional, via a split aggregate hierarchy — `AggregateRoot` vs `EventSourcedAggregateRoot` (ADR-0012, supersedes ADR-0011). Apply ES only where it adds business value.

## Application / CQRS conventions (from accepted ADRs)

- CQRS abstractions and the `Result`/`Failure` model live in `BuildingBlocks.Application` (depends only on `Domain`). A **hand-rolled dispatcher** is used instead of MediatR (ADR-0015); the DI-based implementation (`Sender`, pipeline behaviors) lives in `BuildingBlocks.Infrastructure`.
- Handlers and dispatch are **async-only** with a `CancellationToken`; no sync overloads.
- **Commands** return `Result` or `Result<T>` (create returns the new typed id, e.g. `Result<RecipeId>`; delete/void returns `Result`). **Queries** return `Result<T>`.
- Expected domain errors (`BusinessRuleViolationException`, `DomainValidationException`) are translated to `Result.Failure` by an Application pipeline behavior (`ExceptionToResultBehavior`); unexpected errors bubble to a thin global handler (ADR-0017). `FailureCategory` is one of `Validation`, `BusinessRule`, `NotFound`, `Conflict` — transport status mapping is owned by the BFF/service host, never by `Application`.
- Pipeline behaviors run in explicit DI registration order (see `ServiceCollectionExtensions`).
- Contracts live in the innermost layer that consumes them (ADR-0024).

## Persistence & event sourcing (from accepted ADRs)

- EF Core is the default; Event Sourcing is selective, applied only where the event history carries business value (ADR-0012).
- Everything runs on PostgreSQL (ADR-0020). State-stored contexts use EF Core via Npgsql; event-sourced contexts use Marten on PostgreSQL (ADR-0019).
- Each bounded context owns a write + read database pair (ADR-0021). The write database holds authoritative state (EF Core tables, or Marten event streams); the read database holds query-optimized read models. Databases are never shared across contexts — no cross-database foreign keys, joins, or transactions (cross-context consistency is via integration events). In Aspire, one server resource hosts two `AddDatabase(...)` calls per context (e.g. `nutrition-write`, `nutrition-read`), each with its own connection string. Moving a database to its own server later is a sanctioned, non-breaking migration touching no Domain/Application/Infrastructure code.
- The event store is Marten on PostgreSQL (ADR-0019), used as a **raw event store**: `MartenEventSourcedRepository` appends uncommitted domain events (optimistic concurrency on `Version`) and, on load, fetches the raw stream and folds it through the aggregate's own `LoadFromHistory`. Marten's convention-based `Apply`-on-aggregate aggregation is **not** used, so the domain (ADR-0010/0012) stays untouched.
- The event store and the state-stored store never co-locate in the same database, even on the same server.
- Read models are event-driven via an outbox-backed publisher (ADR-0022), used uniformly for ES and state-stored contexts: domain events flow through Wolverine's transactional outbox in the write transaction; after commit the outbox is flushed immediately (EF Core atomically via `SaveChangesAndFlushMessagesAsync`; Marten via the flush-on-commit listener that `IMartenOutbox.Enroll` registers — the durability agent's polling is crash-recovery only) and `DomainEventEnvelopeHandler` invokes `Publisher`, which dispatches events to in-context `IProjectionHandler`s (via `ProjectionRunner`, updating the read DB) and, where selected, to the integration-event path on RabbitMQ via Wolverine. Integration-event publication is bound to the handler's own `IMessageContext` through the explicit `IIntegrationEventSink` parameter (ADR-0022 amendment; never a DI-resolved bus — that produced duplicates and broke trace correlation, see IMP-04). Wolverine is transport-only, not the CQRS mediator (ADR-0015/0023). Delivery is **at-least-once**, so projection handlers must be idempotent and per-aggregate order-aware (track a last-processed position/version); reads are eventually consistent with writes.
- Read models are owned by each service, not a Building Block — the service owns its read-model schema, projection handlers, and queries; Infrastructure ships only the plumbing (Publisher, outbox, dispatch loop, projection runner, transport). Read models are derived and rebuildable by replaying events / re-running projections.
- In-context projections use domain events directly; integration events (RabbitMQ) are the only cross-context signal — never read another context's database.- Snapshotting is deferred but additive: a Marten snapshot is a separate document and the event schema is unchanged, so snapshots can be added per context later with no event migration.
- PostgreSQL is provisioned as a first-party .NET Aspire resource.

ADRs are immutable once accepted; to change a decision, add a superseding ADR. Index: `docs/architecture/decisions/README.md`.

## XML documentation conventions (ADR-0013)

- **Scope:** XML docs are required **only under `BuildingBlocks/src/*`**. Do not add them to `BuildingBlocks/tests/*` or to any application/service code outside `BuildingBlocks`. Enforced by that folder's `.editorconfig` (`dotnet_diagnostic.CS1591.severity = warning`) — never copy that setting into test or service projects.
- **`<remarks>`** — why / how / when. For every public/protected member, include at most one `<remarks>` covering any _useful_ subset of why it exists, how to use it, when to use it; never restate the `<summary>`. Required on types and on methods/constructors (omit only when nothing beyond the summary can be said). Optional on trivial properties (`Id`, `Message`, `Value`, `IsEmpty`, …) and equality/boilerplate (`Equals`, `GetHashCode`, `==`, `!=`). Exempt for explicitly implemented members using `<inheritdoc/>`.
- **Formatting:** `<summary>` is one sentence; booleans use `<c>true</c> if …; otherwise, <c>false</c>.`; null is always `<see langword="null"/>`; use `<see cref>`/`<typeparamref>`/`<paramref>`; document exceptions with `<exception cref="...">Thrown when …</exception>`.
- **Canonical phrasings** describe the same concept the same way every time (e.g. `TKey` → "The type of the identity key."; `Id` → "Gets the unique identifier of the {entity|aggregate root}."). Full glossary in ADR-0013.

## Testing

- Frameworks: **xUnit** (including built-in `Assert.*`), **NSubstitute**, **EF Core InMemory**. Do **not** use FluentAssertions — removed for licensing reasons (ADR-0014).
- **Testcontainers (PostgreSQL)** backs the `BuildingBlocks.Infrastructure` integration tests (Marten optimistic concurrency, strongly-typed key persistence); guard them with `Skip`/`Assert.SkipUnless` so they skip when Docker is unavailable instead of failing.
- Test projects mirror source structure 1:1 (e.g. `BuildingBlocks.Domain.Tests` for `BuildingBlocks.Domain`); domain tests use lightweight hand-written test doubles (`TestDoubles/`) rather than mocks — the domain has no infrastructure dependencies to mock. NSubstitute is reserved for application/persistence/messaging tests.
- Categories: unit, domain, application-layer, persistence, integration, component-communication. See `docs/architecture/testing-strategy.md`.
- Assert observable behavior, not internal details (e.g. "creating a recipe raises a `RecipeCreated` event").
- Add/extend tests alongside any behavioral change.

## When contributing

1. Put reusable, VitalSync-agnostic concepts in `BuildingBlocks`; put domain logic in the matching `src/Services/<Domain>` project.
2. Respect the communication rules (Frontend → BFF → services; async only between services).
3. Follow the DDD/CQRS/ES ADR conventions above.
4. Keep layer boundaries clean; don't leak infrastructure into the domain.
5. Add or update tests (mirror the project structure).
6. Document `BuildingBlocks/src/*` per the XML documentation conventions (ADR-0013); don't add XML docs to tests or service code.
7. If a change affects architecture, add or update an ADR using the template in `docs/architecture/decisions/README.md`.
8. Match existing style; respect `.editorconfig` and `Directory.Build.props`.
9. Always publish changes directly to the `main` branch.

## Key documentation

- Architecture overview — `docs/architecture/overview.md`
- Communication — `docs/architecture/communication.md`
- Building Blocks — `docs/architecture/building-blocks.md` (+ `-domain.md`, `-application.md`, `-infrastructure.md`)
- Domain model — `docs/architecture/domain-model.md`
- CQRS & Event Sourcing — `docs/architecture/cqrs-and-event-sourcing.md`
- Testing strategy — `docs/architecture/testing-strategy.md`
- ADRs — `docs/architecture/decisions/README.md`
- Glossary — `docs/glossary.md`
- User stories — `docs/userStories/`
