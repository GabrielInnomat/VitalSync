# Claude instructions

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What VitalSync is

A cloud-native, distributed platform unifying **nutrition**, **fitness**, and
**health analytics** behind a single Blazor UI. Built as independent ASP.NET Core
microservices using **DDD**, **CQRS**, and **selective Event Sourcing**.

> Core principle: **the architecture is fixed, the domain is fluid.**
> Technical/architectural decisions are mandatory. Business/domain details are
> refined iteratively. When a change affects architecture, add or update an ADR.

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
│   │   ├── BuildingBlocks.Infrastructure/  # Cross-cutting infrastructure (e.g. DI-based dispatcher, event sourcing via Marten, persistence, messaging)
│   │                                       # outbox, projections, Wolverine/RabbitMQ transport
│   └── tests/                      # Mirrors src/ with *.Tests projects (Domain.Tests, Application.Tests)
├── src/                             # VitalSync APPLICATION
│   ├── Aspire/                      # .NET Aspire AppHost & ServiceDefaults (entry point)
│   ├── Bff/                         # Backend-for-Frontend (REST out, gRPC in)
│   ├── Frontend/VitalSync.Web/      # Blazor client (UI only — no business logic)
│   └── Services/                    # One folder per microservice; Api + MigrationService per context,
│       ├── Nutrition/               #   both still skeletons without domain code
│       │   ├── VitalSync.Nutrition.Api/
│       │   └── VitalSync.Nutrition.MigrationService/
│       ├── Fitness/                 # same two projects
│       └── Analytics/               # same two projects
├── samples/                         # THROWAWAY walking skeleton — see WalkingSkeleton.md
│   ├── VitalSync.Samples.AppHost/   # its own Aspire host; the production one must not depend on it
│   ├── StateStored/                 # EF Core end-to-end slice (Widget)
│   └── EventSourced/                # Marten end-to-end slice (Gadget)
├── docs/architecture/               # Architecture docs, ADRs (decisions/), glossary, user stories
└── tests/                           # Tests for src/ (BuildingBlocks has its own tests/ folder)
    └── VitalSync.ServiceDefaults.Tests/   # NOTE: tests/VitalSync.Tests/ is a broken template
                                           # leftover, deliberately not in VitalSync.slnx
```

Guidance for finding things:

- **Shared/reusable concepts** (base aggregate, domain event, typed IDs, CQRS interfaces, `Result`) → `BuildingBlocks/src/...`. Must stay framework-agnostic and independent of VitalSync itself.
- **Business logic** → the relevant service under `src/Services/<Domain>/`.
- **UI** → `src/Frontend/`.
- **Entry point / running the system** → `src/Aspire/`.
- **How Building Blocks is actually consumed** → `samples/`. It is a deliberately business-empty vertical slice that exists to prove the wiring works, and it is meant to be deleted once it has answered its questions. Never add business value there, and never let production code depend on it. `WalkingSkeleton.md` records what it proved and what is still open.

## Architecture & communication rules (do not violate)

- The Blazor frontend communicates **exclusively** through the **BFF**.
- The BFF exposes **REST** to the frontend and talks to microservices via
  **code-first gRPC**.
- Microservices **never** call each other synchronously. All inter-service
  communication is **asynchronous** via RabbitMQ/Wolverine.
- Layer separation (Domain / Application / Infrastructure / Persistence) is
  mandatory; keep dependencies pointing inward (domain has no infrastructure deps).
- **Contract placement** (ADR-0024): a contract lives in the **innermost layer
  whose language it speaks and that actually consumes it** — decided by its
  _consumer_, not its implementor (implementations always live outside, per DIP).
  Domain vocabulary (`IDomainEvent`, business rules, `IClock`) → `Domain`;
  orchestration-facing contracts (`IRepository`, `IUnitOfWork`,
  projection-handler / event-publisher abstractions, integration-event marker) →
  `Application`; **all implementations** → `Infrastructure`, which defines no
  use-case contracts of its own. New contract? Place it by asking who consumes it.
- **Every service host wires the same defaults** (see any `src/Services/<Domain>/*.Api/Program.cs`):
  `builder.AddServiceDefaults()`, one `AddNpgSqlReadinessCheck` **per database the context
  owns** (`<context>-write` _and_ `<context>-read`), `AddRabbitMqReadinessCheck()`,
  `AddProblemDetails()` + `app.UseExceptionHandler()` (ADR-0017's thin global handler),
  `app.MapDefaultEndpoints()`, and `await app.RunAsync().ConfigureAwait(false)`. The
  connection names **are** the Aspire resource names — `AddNpgSqlReadinessCheck` throws at
  startup when the name is not configured, so renaming a resource in the AppHost fails loudly
  instead of leaving the service permanently unhealthy while the BFF waits on it
  (`tests/VitalSync.ServiceDefaults.Tests`).

See `docs/architecture/communication.md` and the ADRs below.

## Technology stack

| Concern                 | Choice                                                                                                                 |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Orchestration           | .NET Aspire 13                                                                                                         |
| Frontend                | Blazor                                                                                                                 |
| Backend-for-Frontend    | REST (to frontend) + code-first gRPC (to services)                                                                     |
| Microservices           | ASP.NET Core, one per bounded context                                                                                  |
| Inter-service messaging | RabbitMQ via Wolverine                                                                                                 |
| Persistence             | EF Core on PostgreSQL by default; Marten (event sourcing) on PostgreSQL where ES adds value                            |
| Database topology       | PostgreSQL; a **write + read database pair** per bounded context; shared server now, server-per-context possible later |
| Read models             | Event-driven projections in each context's read DB via an outbox-backed publisher                                      |
| Patterns                | DDD, CQRS, Event Sourcing (selective)                                                                                  |
| Testing                 | xUnit (incl. built-in asserts), NSubstitute, EF Core InMemory — **no FluentAssertions** (ADR-0014)                     |

Language: **C#**. Solution file: `VitalSync.slnx`. Shared build config in
`Directory.Build.props` and `.editorconfig`.

## Business domains

- **Nutrition** — ingredients & nutritional values, recipes, meal plans, shopping
  lists, nutrient-intake calculation.
- **Fitness** — exercises, workout plans, workout-session tracking, energy/calorie
  expenditure.
- **Analytics** — insights derived from nutrition and fitness data.

Bounded-context decomposition is iterative — see `docs/architecture/domain-model.md`.

## Domain / DDD conventions

- Use **strongly typed aggregate identifiers** — no raw `Guid`/`int` IDs.
- The **aggregate owns its domain events** (ADR-0006); expose read-only vs. managed
  domain events per ADR-0007.
- **Entity identity and equality** follow ADR-0008.
- **Business rules and domain validation** follow ADR-0009.
- Aggregates use an **aggregate state object** (ADR-0010).
- **One aggregate authoring model** — every aggregate derives from the state-fold
  base `AggregateRoot<TKey, TState>` and mutates only via `RaiseEvent`; the
  **event-sourced base is additive** (`Version` + `LoadFromHistory` only), per
  ADR-0025 (which supersedes ADR-0012). Only apply ES where the event history
  carries business value.
- **EF Core maps the aggregate's _state_, never the aggregate** (ADR-0025 amendment):
  `Id => State.Id` is computed, so it cannot serve as a mapped primary key, and a
  positional record also fails as a `ComplexProperty`. The state record maps as an
  ordinary **entity type** — one table, one id column, no shadow key. Infrastructure
  reaches it through `IStateOwner`, implemented **explicitly** on `AggregateRoot` so
  domain code never sees it and cannot bypass the event fold. A state-stored aggregate
  therefore needs a **parameterless constructor** (may be non-public; the Marten path's
  `new()` requires a public one).
- **One repository contract**: `IRepository<TAggregate, TKey>` with `GetByIdAsync`
  and `AddAsync` only (ADR-0026) — no `Remove` (removal is a soft-delete state
  change), no `Save`/`Update` (retrieved aggregates are tracked; changes flow
  through the unit of work). Both EF Core and Marten implement the same contract, and
  **both track in the repository**: EF's change tracker only ever sees states, so
  `EfCoreAggregateTracker` mirrors `MartenAggregateTracker` (ADR-0026 amendment).
  Consequence to know: **domain events are collected only from aggregates that went
  through `IRepository`** — an entity written straight into the `DbContext` produces
  none.

## Application / CQRS conventions (from accepted ADRs)

- CQRS abstractions and the `Result` / `Failure` model live in
  **`BuildingBlocks.Application`** (depends only on `Domain`). A **hand-rolled
  dispatcher** is used instead of MediatR (ADR-0015); the DI-based implementation
  lives in `BuildingBlocks.Infrastructure`.
- Handlers and dispatch are **async-only** with a `CancellationToken`; no sync overloads.
- **Handler registration** via `BuildingBlocksOptions.AddHandlersFrom(assembly)` is
  idempotent for multi-handler contracts (`IProjectionHandler<>`,
  `IIntegrationEventMapper`) and enforces **exactly one** handler per command/query —
  two different handlers for the same `ICommand`/`IQuery` throw at registration, not
  at request time. **Startup handler validation is on by default**: a hosted service
  registered by `AddBuildingBlocks` verifies at host start that every command/query
  in the scanned assemblies resolves to a handler (fail-fast instead of
  "no service registered" on the first request) and rejects request types that
  implement **more than one** `ICommand<>`/`IQuery<>` contract — a command or query
  has exactly one result type; opt out only deliberately via
  `options.ValidateHandlersOnStart = false`.
- **Commands** return `Result` or `Result<T>` (a **create** returns the new typed id,
  e.g. `Result<RecipeId>`; **delete/void** returns `Result`). **Queries** return `Result<T>`.
- Expected domain errors (`BusinessRuleViolationException`, `DomainValidationException`)
  are **translated to `Result.Failure`** by an Application pipeline behavior; unexpected
  errors bubble to a thin global handler (ADR-0017). `FailureCategory` is one of
  `Validation`, `BusinessRule`, `NotFound`, `Conflict` — transport status mapping is
  owned by the BFF/service host, never by `Application`.
- Pipeline behaviors run in an **explicit numeric order**: logging is outermost so
  expected domain errors are logged as `Warning` (not `Error`), then exception-to-`Result`
  translation, then the unit of work closest to the handler. Built-ins occupy fixed
  slots; services add their own via `BuildingBlocksOptions.AddPipelineBehavior(type, order)`
  (negative runs before built-ins, higher runs after).

## Persistence & event sourcing (from accepted ADRs)

- EF Core is the default; Event Sourcing is selective, applied only where the event history carries business value (ADR-0012).
- **Everything runs on PostgreSQL** — the single relational engine (ADR-0020).
  State-stored contexts use **EF Core via the Npgsql provider**; event-sourced
  contexts use **Marten on PostgreSQL** (ADR-0019).
- **Each bounded context owns a write + read database pair** (ADR-0021). The **write
  database** holds the authoritative state (EF Core tables, or Marten event streams);
  the **read database** holds query-optimized read models. Databases are **never
  shared across contexts**, with **no cross-database foreign keys, joins, or
  transactions** (cross-context consistency is via integration events). Today all
  context databases live on **one shared PostgreSQL server** (in Aspire: one server
  resource with **two `AddDatabase(...)` calls per context**, e.g. `nutrition-write`
  and `nutrition-read`, each with its own named connection string). Moving either
  database of a context onto its **own dedicated server** later is a sanctioned,
  non-breaking migration — a connection-string change plus a data move, touching no
  Domain/Application/Infrastructure code (ADR-0020/0021).
  The **production AppHost does exactly this today** for all three contexts
  (`src/Aspire/VitalSync.AppHost/AppHost.cs`) and follows the walking skeleton's
  migration pattern: **one `MigrationService` worker per context**, referenced by both
  databases, and the service starts only after it via `.WaitForCompletion(...)`.
  A new context therefore means: two `AddDatabase(...)` calls, one migration worker,
  one service — never a single shared database.
- **State-stored contexts persist the aggregate's state object**, not the aggregate
  (ADR-0025/0026 amendments): `EfCoreRepository` loads the state via
  `FindAsync(stateType, [id])` and rehydrates an empty aggregate around it, and the
  `EfCoreUnitOfWork` copies the current state onto the tracked entry with
  `CurrentValues.SetValues` before saving — states are immutable, so without that copy
  EF Core finds nothing to save and the change is silently lost.
- The **event store is Marten on PostgreSQL** (ADR-0019), used as a **raw event store**:
  the event-sourced repository in `BuildingBlocks.Infrastructure` tracks aggregates
  (loaded and added) and the Marten unit of work appends their uncommitted domain
  events at commit (optimistic concurrency on `Version`); on load, the repository
  fetches the raw stream and folds it through the aggregate's own `LoadFromHistory`.
  Marten's convention-based `Apply`-on-aggregate aggregation is **not** used, so the
  domain (ADR-0010 / ADR-0025) stays untouched.
- The **event store and the state-stored store never co-locate in the same database**,
  even on the same server, so they can move and scale independently.
- **Read models are event-driven, via an outbox-backed publisher** (ADR-0022), used
  uniformly for ES and state-stored contexts: domain events are written to a
  **transactional outbox** in the write transaction; after commit the **Publisher**
  (in `BuildingBlocks.Infrastructure`) drains the outbox and dispatches events to
  **in-context projection handlers** (updating the read DB) and, where selected, to
  the **integration-event path** on RabbitMQ via **Wolverine** (the same outbox
  already required for integration events is reused; Wolverine is transport-only,
  **not** the CQRS mediator — ADR-0015/0023). Integration-event publication is
  **bound to the handler's own `IMessageContext`** via the explicit
  `IIntegrationEventSink` parameter on `IDomainEventPublisher` (ADR-0022
  amendment) — never a DI-resolved `IMessageBus`, which produces an un-enrolled
  context (duplicates on redelivery, broken trace correlation). Both persistence
  paths **flush the outbox immediately after a successful commit** (EF Core
  atomically via `SaveChangesAndFlushMessagesAsync`; Marten via the flush-on-commit
  listener that `IMartenOutbox.Enroll` registers) — the durability agent's polling
  is crash-recovery only. Delivery is **at-least-once**, so projection handlers
  **must be idempotent and per-aggregate order-aware** (track a last-processed
  position/version); reads are **eventually consistent** with writes.
- **Read models are owned by each service, not a Building Block** — the service owns
  its read-model schema, projection handlers, and queries; Infrastructure ships only
  the plumbing (Publisher, outbox, dispatch loop, projection runner, transport). Read
  models are **derived and rebuildable** by replaying events / re-running projections.
- **In-context** projections use **domain** events directly; **integration** events
  (RabbitMQ) are the **only** cross-context signal — never read another context's
  database.
- **Broker topology** (ADR-0023 amendment): one topic exchange
  `vitalsync.integration-events` for the whole platform. The publishing rule matches
  `MessagesImplementing<IIntegrationEvent>()` — **never** all messages, so
  `DomainEventEnvelope` cannot leak onto the broker. Every integration event **must**
  carry `[Topic("<context>.<event>")]` in kebab-case (`nutrition.recipe-created`): the
  routing key is part of the published contract, not derived from the CLR namespace.
  Consumers subscribe via `options.SubscribeToIntegrationEvents(queue, consumerAssembly,
  patterns)` — Building Blocks wires **both halves**, so the subscribing host adds
  nothing of its own. Pass the service's **Infrastructure** assembly,
  never its Application assembly: Wolverine discovers handlers by naming convention and
  would mistake `CreateRecipeHandler` for a message handler. Beware: Wolverine
  **silently discards** a message with no route, and a message whose consumer was never
  discovered is marked handled and dropped without a retry or a dead letter — both
  failures are invisible. Note that `nutrition.*` also matches the subscriber's own
  published events. A consumer that keeps throwing is retried three times and the message
  then goes to Wolverine's `wolverine-dead-letter-queue` **on the broker** — not to the
  `wolverine_dead_letters` table in the write database, which stays empty (`DeadLetterTests`).
- **Snapshotting is deferred** but additive: a Marten snapshot is a separate document
  and the event schema is unchanged, so snapshots can be added per context later with
  **no event migration**.
- **A service host registers through the host-builder overload** `builder.AddBuildingBlocks(options => …, configureWolverine?)` (ADR-0027 amendment 2026-08-03) and calls **no `UseWolverine` at all** — Building Blocks issues it, and applies the EF Core outbox from the write connection string the host already named in `UseEfCorePersistence`. **The write database is named exactly once**; the earlier requirement to repeat it in the host's own `UseWolverine(...)` is gone, and with it the silent failure of outbox and aggregates landing in different databases. Wolverine permits only one `UseWolverine`, so host-specific transport settings go in the optional `configureWolverine` callback. This is the **only** way to get the EF Core outbox — the former public `UseBuildingBlocksEfCorePersistence(cs)` is deleted, so no host can point the message store at a second database. The `IServiceCollection` overload still serves handlers, Marten, and messaging for hosts that wire Wolverine themselves; a **state-stored** host must use the builder overload.
- PostgreSQL is provisioned as a first-party **.NET Aspire** resource.

ADRs are immutable once accepted; to change a decision, add a superseding ADR.
Index: `docs/architecture/decisions/README.md`.

## XML documentation conventions (ADR-0013)

- **Scope:** XML docs are required **only under `BuildingBlocks/src/*`**. Do not add them to `BuildingBlocks/tests/*` or to any application/service code outside `BuildingBlocks`. Enforced by that folder's `.editorconfig` (`dotnet_diagnostic.CS1591.severity = warning`) — never copy that setting into test or service projects.
- **`<remarks>`** — why / how / when. For every public/protected member, include at most one `<remarks>` covering any _useful_ subset of why it exists, how to use it, when to use it; never restate the `<summary>`. Required on types and on methods/constructors (omit only when nothing beyond the summary can be said). Optional on trivial properties (`Id`, `Message`, `Value`, `IsEmpty`, …) and equality/boilerplate (`Equals`, `GetHashCode`, `==`, `!=`). Exempt for explicitly implemented members using `<inheritdoc/>`.
- **Formatting:** `<summary>` is one sentence; booleans use `<c>true</c> if …; otherwise, <c>false</c>.`; null is always `<see langword="null"/>`; use `<see cref>`/`<typeparamref>`/`<paramref>`; document exceptions with `<exception cref="...">Thrown when …</exception>`.
- **Canonical phrasings** describe the same concept the same way every time (e.g. `TKey` → "The type of the identity key."; `Id` → "Gets the unique identifier of the {entity|aggregate root}."). Full glossary in ADR-0013.

## Testing

- Frameworks: **xUnit** (including built-in `Assert.*`), **NSubstitute**, **EF Core InMemory**. Do **not** use FluentAssertions — removed for licensing reasons (ADR-0014).
- **Testcontainers (PostgreSQL)** backs the `BuildingBlocks.Infrastructure` integration tests (Marten optimistic concurrency, strongly-typed key persistence); guard them with `Skip`/`Assert.SkipUnless` so they skip when Docker is unavailable instead of failing.
- Test projects mirror source structure 1:1 (e.g. `BuildingBlocks.Domain.Tests` for `BuildingBlocks.Domain`); domain tests use lightweight hand-written test doubles (`TestDoubles/`) rather than mocks — the domain has no infrastructure dependencies to mock. NSubstitute is reserved for application/persistence/messaging tests.
- Assert observable behavior, not internal details (e.g. "creating a recipe raises a `RecipeCreated` event").
- Add/extend tests alongside any behavioral change.
- Fixture types that must live **outside** the test assembly (e.g. for assembly-scanning
  tests) go in a dedicated project under
  `BuildingBlocks/tests/ExternalAssemblies/<ShortName>Fixture/` — keep folder/project names
  **short** (Windows `MAX_PATH`), reference them from the test project via `ProjectReference`,
  add them to `VitalSync.slnx`, no XML docs. See `docs/architecture/testing-strategy.md`.
- Strategy covers unit, integration, domain, application-layer, persistence, and
  component-communication tests. See `docs/architecture/testing-strategy.md`.

## When contributing

1. Put reusable, VitalSync-agnostic concepts in `BuildingBlocks`; put domain logic in the matching `src/Services/<Domain>` project.
2. Respect the communication rules (Frontend → BFF → services; async only between services).
3. Follow the DDD/CQRS/ES ADR conventions above.
4. Keep layer boundaries clean; don't leak infrastructure into the domain.
5. Add or update tests (mirror the project structure).
6. Document `BuildingBlocks/src/*` per the XML documentation conventions (ADR-0013); don't add XML docs to tests or service code.
7. If a change affects architecture, add or update an ADR using the template in `docs/architecture/decisions/README.md`.
8. Match existing style; respect `.editorconfig` and `Directory.Build.props`.
9. **Always commit and push directly to the `main` branch** — never work on separate branches, and never ask which branch to use; `main` is always the target.
10. Always update the instruction files in `.github/*.md` and `.claude/*.md` if you discover a gap or ambiguity in the guidance.
11. If you are unsure about a decision, **always ask a human** — Copilot is not the arbiter of architecture or domain rules.
12. Always use short and clear commit messages.
13. If you write code, always add or update unit tests / integration tests / architecture tests, and make sure they pass before committing.
14. Always check all `*.md` files in the repository and update them if needed.

## Key documentation

- Architecture overview — `docs/architecture/overview.md`
- Communication — `docs/architecture/communication.md`
- Building Blocks — `docs/architecture/building-blocks.md`
- Domain model — `docs/architecture/domain-model.md`
- CQRS & Event Sourcing — `docs/architecture/cqrs-and-event-sourcing.md`
- Testing strategy — `docs/architecture/testing-strategy.md`
- ADRs — `docs/architecture/decisions/README.md`
- Glossary — `docs/glossary.md`
