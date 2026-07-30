# Copilot instructions — VitalSync

These instructions orient Copilot (chat and coding agent) so it can navigate and
contribute to VitalSync **without rediscovering the architecture file-by-file**.
Read this first, then consult the linked docs before making changes.

## What VitalSync is

A cloud-native, distributed platform unifying **nutrition**, **fitness**, and
**health analytics** behind a single Blazor UI. Built as independent ASP.NET Core
microservices using **DDD**, **CQRS**, and **selective Event Sourcing**.

> Core principle: **the architecture is fixed, the domain is fluid.**
> Technical/architectural decisions are mandatory. Business/domain details are
> refined iteratively. When a change affects architecture, add or update an ADR.

## Tech stack

| Concern                 | Choice                                                    |
| ----------------------- | --------------------------------------------------------- |
| Orchestration           | .NET Aspire 13                                            |
| Frontend                | Blazor (UI only)                                         |
| Backend-for-Frontend    | REST (to frontend) + code-first gRPC (to services)      |
| Microservices           | ASP.NET Core, one per business area                      |
| Inter-service messaging | RabbitMQ via Wolverine (MIT; runs side-by-side with Marten, ADR-0023) |
| Persistence             | EF Core on PostgreSQL; Event Sourcing via Marten on PostgreSQL where it adds business value (ADR-0019/0020) |
| Database topology       | PostgreSQL; a **write + read database pair** per bounded context (ADR-0021); shared server now, server-per-context possible later (ADR-0020) |
| Read models             | Event-driven projections in each context's read DB via an outbox-backed publisher (ADR-0022) |
| Patterns                | DDD, CQRS, Event Sourcing (selective)                    |
| Testing                 | xUnit (incl. built-in asserts), NSubstitute, EF Core InMemory |

Language: **C#**. Solution file: `VitalSync.slnx`. Shared build config in
`Directory.Build.props` and `.editorconfig`.

## Repository map (where to look)

```text
VitalSync/
├── BuildingBlocks/                 # Reusable, VitalSync-INDEPENDENT platform
│   ├── src/
│   │   ├── BuildingBlocks.Domain/          # Aggregates, entities, domain events, IDs, rules
│   │   ├── BuildingBlocks.Application/      # CQRS abstractions (commands/queries/handlers), Result/Failure
│   │   ├── BuildingBlocks.Infrastructure/  # Cross-cutting infrastructure (e.g. DI-based dispatcher, event sourcing via Marten, persistence, messaging)
│   └── tests/                          # Mirrors src/ with *.Tests projects
├── src/                            # VitalSync APPLICATION
│   ├── Aspire/                     # .NET Aspire AppHost & ServiceDefaults (entry point)
│   ├── Bff/                        # Backend-for-Frontend (REST out, gRPC in)
│   ├── Frontend/                   # Blazor client (UI only)
│   └── Services/                   # One folder per microservice
│       ├── Nutrition/
│       ├── Fitness/
│       └── Analytics/
├── docs/                           # Architecture, ADRs, glossary, user stories
└── tests/                          # Cross-cutting / integration tests
```

Guidance for finding things:
- **Shared/reusable concepts** (base aggregate, domain event, typed IDs, CQRS
  interfaces, `Result`) → `BuildingBlocks/src/...`. These must stay framework-agnostic and
  independent of VitalSync.
- **Business logic** → the relevant service under `src/Services/<Domain>/`.
- **UI** → `src/Frontend/` (never put business logic here).
- **Entry point / running the system** → `src/Aspire/`.

## Business domains

- **Nutrition** — ingredients & nutritional values, recipes, meal plans, shopping
  lists, nutrient-intake calculation.
- **Fitness** — exercises, workout plans, workout-session tracking, energy/calorie
  expenditure.
- **Analytics & Reporting** — insights derived from nutrition and fitness data.

Bounded-context decomposition is iterative — see `docs/architecture/domain-model.md`.

## Architecture & communication rules (do not violate)

- The Blazor frontend communicates **exclusively** through the **BFF**.
- The BFF exposes **REST** to the frontend and talks to microservices via
  **code-first gRPC**.
- Microservices **never** call each other synchronously. All inter-service
  communication is **asynchronous** via RabbitMQ/Wolverine .
- Layer separation (Domain / Application / Infrastructure / Persistence) is
  mandatory; keep dependencies pointing inward (domain has no infrastructure deps).

See `docs/architecture/communication.md` and the ADRs below.

## Domain / DDD conventions (from accepted ADRs)

- Use **strongly typed aggregate identifiers** (ADR-0005) — no raw `Guid`/`int` IDs.
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
- **One repository contract**: `IRepository<TAggregate, TKey>` with `GetByIdAsync`
  and `AddAsync` only (ADR-0026) — no `Remove` (removal is a soft-delete state
  change), no `Save`/`Update` (retrieved aggregates are tracked; changes flow
  through the unit of work). Both EF Core and Marten implement the same contract.

## Application / CQRS conventions (from accepted ADRs)

- CQRS abstractions and the `Result` / `Failure` model live in
  **`BuildingBlocks.Application`** (depends only on `Domain`). A **hand-rolled
  dispatcher** is used instead of MediatR (ADR-0015); the DI-based implementation
  lives in `BuildingBlocks.Infrastructure`.
- Handlers and dispatch are **async-only** with a `CancellationToken`; no sync overloads.
- **Commands** return `Result` or `Result<T>` (a **create** returns the new typed id,
  e.g. `Result<RecipeId>`; **delete/void** returns `Result`). **Queries** return `Result<T>`.
- Expected domain errors (`BusinessRuleViolationException`, `DomainValidationException`)
  are **translated to `Result.Failure`** by an Application pipeline behavior; unexpected
  errors bubble to a thin global handler (ADR-0017). `FailureCategory` is one of
  `Validation`, `BusinessRule`, `NotFound`, `Conflict` — transport status mapping is
  owned by the BFF/service host, never by `Application`.
- Pipeline behaviors run in **explicit DI registration order**.

## Persistence & event sourcing (from accepted ADRs)

- **EF Core is the default**; **Event Sourcing is selective**, applied only where the
  event history carries business value (ADR-0025).
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
  **not** the CQRS mediator — ADR-0015/0023). Delivery is **at-least-once**, so
  projection handlers **must
  be idempotent and per-aggregate order-aware** (track a last-processed
  position/version); reads are **eventually consistent** with writes.
- **Read models are owned by each service, not a Building Block** — the service owns
  its read-model schema, projection handlers, and queries; Infrastructure ships only
  the plumbing (Publisher, outbox, dispatch loop, projection runner, transport). Read
  models are **derived and rebuildable** by replaying events / re-running projections.
- **In-context** projections use **domain** events directly; **integration** events
  (RabbitMQ) are the **only** cross-context signal — never read another context's
  database.
- **Snapshotting is deferred** but additive: a Marten snapshot is a separate document
  and the event schema is unchanged, so snapshots can be added per context later with
  **no event migration**.
- PostgreSQL is provisioned as a first-party **.NET Aspire** resource.

ADRs are immutable once accepted; to change a decision, add a superseding ADR.
Index: `docs/architecture/decisions/README.md`.

## XML documentation conventions (ADR-0013)

XML documentation is authored to a consistent standard — see
`docs/architecture/decisions/0013-xml-documentation-conventions.md`.

- **Scope:** XML docs are required **only under `BuildingBlocks/src/*`**. Do **not** add
  them to `BuildingBlocks/tests/*` or to any application/service code outside
  `BuildingBlocks`. The requirement is enforced by the `BuildingBlocks` `.editorconfig`
  (`dotnet_diagnostic.CS1591.severity = warning`); never copy that setting into test or
  service projects.
- **`<remarks>` — why / how / when.** For every public/protected member, include **at most
  one** `<remarks>` covering any *useful* subset of: **why** it exists, **how** to use it,
  **when** to use it. Include only the parts that add insight; never restate the `<summary>`.
  - Required on **types** and on **methods/constructors** (omit only in the rare case where
    nothing beyond the summary can be said).
  - Optional on **trivial properties** (`Id`, `Message`, `Value`, `IsEmpty`, …) and
    **equality/boilerplate** (`Equals`, `GetHashCode`, `==`, `!=`) — add only when insightful.
  - Exempt for explicitly implemented members using `<inheritdoc/>`.
- **Formatting:** `<summary>` is one sentence; booleans use `<c>true</c> if …; otherwise, <c>false</c>.`;
  null is always `<see langword="null"/>`; use `<see cref>` / `<typeparamref>` / `<paramref>` for
  references; document exceptions with `<exception cref="...">Thrown when …</exception>`.
- **Canonical phrasings:** describe the same concept the same way every time (e.g. `TKey` →
  "The type of the identity key."; `Id` → "Gets the unique identifier of the {entity|aggregate root}.").
  See ADR-0013 for the full glossary.

## Testing

- Frameworks: **xUnit** (including its built-in `Assert.*` assertions), **NSubstitute**,
  **EF Core InMemory**. Do **not** use FluentAssertions — it was removed for licensing
  reasons (see ADR-0014); express expectations with standard xUnit assertions.
- Test projects mirror source structure (e.g. `BuildingBlocks.Domain.Tests`).
- Strategy covers unit, integration, domain, application-layer, persistence, and
  component-communication tests. See `docs/architecture/testing-strategy.md`.
- Add/extend tests alongside any behavioral change.

## Build & run

```bash
dotnet build                                          # build the solution
dotnet run --project src/Aspire/VitalSync.AppHost     # run via Aspire AppHost
dotnet test                                           # run tests
```

Prerequisites: .NET SDK (aligned with Aspire 13), the .NET Aspire 13 workload, and
Docker (for messaging infrastructure/containers).

## When contributing (checklist for Copilot)

1. Put reusable, VitalSync-agnostic concepts in `BuildingBlocks`; put domain logic
   in the matching `src/Services/<Domain>` project.
2. Respect the communication rules (Frontend → BFF → services; async between services).
3. Follow the DDD/CQRS/ES ADR conventions listed above.
4. Keep layer boundaries clean; don't leak infrastructure into the domain.
5. Add or update tests (mirror the project structure).
6. Document `BuildingBlocks/src/*` per the XML documentation conventions (ADR-0013);
   don't add XML docs to tests or service code.
7. If a change affects architecture, **add or update an ADR** using the template in
   `docs/architecture/decisions/README.md`.
8. Match existing style; respect `.editorconfig` and `Directory.Build.props`.
9. **do never push or commit directly to the `main` branch. **
never work on separate branches, and never ask which
   branch to use — `main` is always the target. keep the changes local.

## Key documentation

- Architecture overview — `docs/architecture/overview.md`
- Communication — `docs/architecture/communication.md`
- Building Blocks — `docs/architecture/building-blocks.md`
- Domain model — `docs/architecture/domain-model.md`
- CQRS & Event Sourcing — `docs/architecture/cqrs-and-event-sourcing.md`
- Testing strategy — `docs/architecture/testing-strategy.md`
- ADRs — `docs/architecture/decisions/README.md`
- Glossary — `docs/glossary.md`
- User stories — `docs/userStories/`
