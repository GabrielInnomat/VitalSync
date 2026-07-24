# Copilot instructions — VitalSync

These instructions orient Copilot (chat and coding agent) so it can navigate and
contribute to VitalSync **without rediscovering the architecture file-by-file**.
Read this first, then consult the linked docs before making changes.

> [!IMPORTANT]
> **Publishing / branching policy:** Cloud/coding agents are **disabled** for this
> repository. **Always commit directly to the `main` branch.** Never create a new
> branch, never open a pull request, and never ask which branch to use — `main` is
> always the correct and only target. (Note: the tooling will still show a one-time
> write-confirmation prompt for each commit; that is a platform safety gate and is
> expected.)

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
| Inter-service messaging | RabbitMQ via MassTransit                                 |
| Persistence             | EF Core; Event Sourcing where it adds business value     |
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
│   │   ├── BuildingBlocks.Application/      # CQRS abstractions (commands/queries/handlers), Result/Error
│   │   ├── BuildingBlocks.Infrastructure/  # Cross-cutting infrastructure (e.g. DI-based dispatcher)
│   │   └── BuildingBlocks.EventProcessing/  # Event sourcing / event processing
│   │   ├── BuildingBlocks.Persistence/      # EF Core persistence building blocks
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
  communication is **asynchronous** via RabbitMQ/MassTransit.
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
- **Event sourcing is optional**, via a split aggregate hierarchy (ADR-0012, which
  supersedes ADR-0011). Only apply ES where it adds business value.

## Application / CQRS conventions (from accepted ADRs)

- CQRS abstractions and the `Result` / `Error` model live in
  **`BuildingBlocks.Application`** (depends only on `Domain`). A **hand-rolled
  dispatcher** is used instead of MediatR (ADR-0015); the DI-based implementation
  lives in `BuildingBlocks.Infrastructure`.
- Handlers and dispatch are **async-only** with a `CancellationToken`; no sync overloads.
- **Commands** return `Result` or `Result<T>` (a **create** returns the new typed id,
  e.g. `Result<RecipeId>`; **delete/void** returns `Result`). **Queries** return `Result<T>`.
- Expected domain errors (`BusinessRuleViolationException`, `DomainValidationException`)
  are **translated to `Result.Failure`** by an Application pipeline behavior; unexpected
  errors bubble to a thin global handler (ADR-0017). `ErrorCategory` is one of
  `Validation`, `BusinessRule`, `NotFound`, `Conflict` — transport status mapping is
  owned by the BFF/service host, never by `Application`.
- Pipeline behaviors run in **explicit DI registration order**.
- `BuildingBlocks.Common` **does not exist** (ADR-0016) — do not reference it.

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
9. **Always publish changes directly to the `main` branch.** Cloud agents are disabled,
   so never open pull requests, never work on separate branches, and never ask which
   branch to use — `main` is always the target.

## Key documentation

- Architecture overview — `docs/architecture/overview.md`
- Communication — `docs/architecture/communication.md`
- Building Blocks — `docs/architecture/building-blocks.md`
- BuildingBlocks.Application — `docs/architecture/building-blocks-application.md`
- Domain model — `docs/architecture/domain-model.md`
- CQRS & Event Sourcing — `docs/architecture/cqrs-and-event-sourcing.md`
- Testing strategy — `docs/architecture/testing-strategy.md`
- ADRs — `docs/architecture/decisions/README.md`
- Glossary — `docs/glossary.md`
- User stories — `docs/userStories/`
