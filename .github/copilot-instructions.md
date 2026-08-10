# Copilot instructions

This file provides guidance to Copilot (chat and coding agent) when working with code in this repository.

It is a **map, not a manual**. The binding detail lives in `docs/architecture/` and in the ADRs;
this file tells you which of them applies before you touch something, and carries only the rules
that are documented nowhere else.

## What VitalSync is

A cloud-native, distributed platform unifying **nutrition**, **fitness**, and **health analytics**
behind a single Blazor UI. Built as independent ASP.NET Core microservices using **DDD**, **CQRS**,
and **selective Event Sourcing**.

> Technical/architectural decisions are mandatory. Business/domain details are refined iteratively.
> When a change affects architecture, add or update an ADR.

## Build, test, run

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~AggregateRootTests"
dotnet test BuildingBlocks/tests/BuildingBlocks.Domain.Tests
dotnet run --project src/Aspire/VitalSync.AppHost
```

Prerequisites: the .NET SDK pinned in `global.json` (`10.0.302`, `rollForward: latestFeature`) and
Docker. **No Aspire workload** — the AppHosts reference `Aspire.AppHost.Sdk` as a package.

`Directory.Build.props` applies solution-wide: nullable + implicit usings, `latest-all` analysis
level, **warnings as errors**. Language: **C#**; solution file `VitalSync.slnx`.

**Package versions are central** (`Directory.Packages.props`, with `CentralPackageTransitivePinningEnabled`):
a `.csproj` carries `<PackageReference Include="..." />` with **no `Version`**, and a new package
needs a `<PackageVersion>` entry there first — otherwise restore fails (NU1010) instead of drifting
silently. Never reintroduce a per-project `Version`. Two versions stay outside because NuGet cannot
manage them: the SDK pin in `global.json` and `Aspire.AppHost.Sdk` in each AppHost's `<Project Sdk="...">`.

**Analyzer relaxations are rare and deliberate.** Beside the root `.editorconfig` there are exactly
**two** under `BuildingBlocks/src` — `BuildingBlocks.Domain` relaxes `CA1033` (`IDomainEventOwner`/
`IStateOwner` are implemented explicitly on purpose, ADR-0007) and `BuildingBlocks.Application`
relaxes `CA1000` (`Result<T>` needs static factories) — plus one for the test projects. Test-only
relaxations that no `.editorconfig` covers live in the test `.csproj` as `NoWarn`.
**`BuildingBlocks.Infrastructure` has none and needs none**: it carries the full analyzer set
unrelaxed. A new suppression there is a smell worth arguing about first.

## Repository map

```text
VitalSync/
├── BuildingBlocks/                 Reusable, VitalSync-INDEPENDENT platform (own tests/ folder)
│   └── src/
│       ├── BuildingBlocks.Domain/          aggregates, entities, domain events, typed IDs, rules
│       ├── BuildingBlocks.Application/     CQRS abstractions, Result/Failure
│       └── BuildingBlocks.Infrastructure/  dispatcher, persistence, outbox, projections, transport
├── src/                            VitalSync APPLICATION
│   ├── Aspire/                     AppHost & ServiceDefaults (entry point)
│   ├── Bff/                        Backend-for-Frontend (REST out, gRPC in)
│   ├── Frontend/VitalSync.Web/     Blazor client (UI only — no business logic)
│   └── Services/                   Nutrition | Fitness | Analytics, each Api + MigrationService
├── samples/                        THROWAWAY walking skeleton: StateStored (EF Core) + EventSourced (Marten)
├── docs/architecture/              architecture docs, ADRs (decisions/), glossary, user stories
└── tests/                          tests for src/
```

- **Reusable, VitalSync-agnostic concepts** → `BuildingBlocks/src/…`, framework-agnostic.
- **Business logic** → `src/Services/<Domain>/`. **UI** → `src/Frontend/`. **Running it** → `src/Aspire/`.
- **How Building Blocks is actually consumed** → `samples/`. A deliberately business-empty vertical
  slice that proves the wiring works, meant to be deleted once it has answered its questions. Never
  add business value there, and never let production code depend on it.

## Non-negotiable rules

- The Blazor frontend talks **exclusively** to the **BFF**. The BFF exposes **REST** to the frontend
  and **code-first gRPC** to the services.
- Microservices **never** call each other synchronously — only async via RabbitMQ/Wolverine — and
  **never** read another context's database.
- Layer separation is mandatory; dependencies point inward. A contract lives in the innermost layer
  that *consumes* it, implementations always in `Infrastructure` (ADR-0024).
- Each bounded context owns a **write + read database pair**, never shared, no cross-database FKs,
  joins or transactions (ADR-0021).
- `AddBuildingBlocks` is called **exactly once** per host, through the host-builder overload; a host
  never calls `UseWolverine` itself (ADR-0027).
- **Every service host wires the same defaults** (see any `src/Services/<Domain>/*.Api/Program.cs`):
  `builder.AddServiceDefaults()`, one `AddNpgSqlReadinessCheck` **per database the context owns**
  (`<context>-write` *and* `<context>-read`), `AddRabbitMqReadinessCheck()`, `AddProblemDetails()` +
  `app.UseExceptionHandler()` (ADR-0017's thin global handler), `app.MapDefaultEndpoints()`, and
  `await app.RunAsync().ConfigureAwait(false)`. The connection names **are** the Aspire resource
  names. `AddServiceDefaults()` already registers the OpenTelemetry sources `BuildingBlocks`,
  `Npgsql`, `Wolverine` and `Marten` — do not re-add them.
- **No comments** — not in `*.cs`, `*.csproj`, workflow YAML, or code examples in `*.md` (ADR-0028).
- **No FluentAssertions** — xUnit built-in asserts only (ADR-0014).

## Technology stack

| Concern                 | Choice                                                                       |
| ----------------------- | ---------------------------------------------------------------------------- |
| Orchestration           | .NET Aspire 13                                                               |
| Frontend / BFF          | Blazor; BFF with REST (out) + code-first gRPC (in)                           |
| Microservices           | ASP.NET Core, one per bounded context                                        |
| Inter-service messaging | RabbitMQ via Wolverine (transport only, **not** the CQRS mediator)           |
| Persistence             | EF Core on PostgreSQL by default; Marten on PostgreSQL where ES adds value   |
| Database topology       | write + read database pair per context; shared server now, per-context later |
| Read models             | event-driven projections in the read DB via an outbox-backed publisher       |
| Testing                 | xUnit, NSubstitute, EF Core InMemory, Testcontainers                         |

## Business domains

- **Nutrition** — ingredients & nutritional values, recipes, meal plans, shopping lists,
  nutrient-intake calculation.
- **Fitness** — exercises, workout plans, workout-session tracking, energy/calorie expenditure.
- **Analytics** — insights derived from nutrition and fitness data.

Bounded-context decomposition is iterative — see `docs/architecture/domain-model.md`.

## Before you change X, read Y

The ADRs are binding and immutable once accepted; to change a decision, add a superseding one.
Read the listed sources **before** editing, not after a review comment.

| You are touching… | Binding sources |
| ----------------- | --------------- |
| An aggregate, its state, a child entity | ADR-0005 to 0008, 0010, 0025, 0030, 0031, 0032 · `building-blocks-domain.md` |
| A domain event or its persisted name | ADR-0006, 0007, 0029, 0030 · `building-blocks-domain.md` |
| A business rule or domain validation | ADR-0009 · `building-blocks-domain.md` |
| A command, query, handler or pipeline behavior | ADR-0015, 0016, 0024, 0027 · `building-blocks-application.md` |
| `Result`, `Failure`, error translation, transport status | ADR-0017 · `building-blocks-application.md` |
| A repository, unit of work or `DbContext` mapping | ADR-0021, 0026, 0031, 0033 · `building-blocks-infrastructure.md` |
| Typed-key serialization or a persisted field name | ADR-0034, 0035 · `building-blocks-infrastructure.md` |
| Persistence technology or database topology | ADR-0019, 0020, 0021 · `cqrs-and-event-sourcing.md` |
| A projection, read model or its rebuild | ADR-0022, 0036 · `building-blocks-infrastructure.md` |
| An integration event, broker topology, retries, idempotency | ADR-0023 · `communication.md` |
| Schema or broker provisioning, a MigrationService | ADR-0037 · `cqrs-and-event-sourcing.md` |
| DI wiring, start-up checks, the public surface of Infrastructure | ADR-0018, 0027, 0037 · `building-blocks-infrastructure.md` |
| The BFF, gRPC contracts or the AppHost | ADR-0002, 0003 · `communication.md` |
| Tests, fixtures or CI | `testing-strategy.md` |

Index and template: `docs/architecture/decisions/README.md`. Overview: `docs/architecture/overview.md`.
Vocabulary: `docs/glossary.md`.

## Conventions that no ADR carries

- **Folder = namespace, and in `Domain` and `Application` the namespaces are a contract.** Every
  type there is public, so moving a file changes an exported `FullName` and breaks consumers;
  `PublicSurfaceTests` pins the exported-type list in both. In `Infrastructure` the default is
  `internal` and the folder cut carries meaning instead — see `building-blocks-infrastructure.md`.
- **Never name a folder or type after a vendor whose namespace you use.** A `Persistence/Marten/`
  folder breaks every `using Marten;` inside it. Same for type names a vendor already owns:
  the dispatcher is `RequestSender`, the publication step is `DomainEventPublisher`.
- **A service declares shared usings once in its `.csproj`** (`<Using Include="BuildingBlocks.Domain.Aggregates" />`),
  not per file — the sample Domain/Application projects show it.
- **Method naming:** every awaitable contract method carries the `Async` suffix. The one exception
  is a method that satisfies **Wolverine's** discovery convention (`…Handler.Handle`) — renaming
  that silently stops discovery, with no compiler error and no dead letter.
- **A start-up check is never optional.** Each exists because the failure it catches is otherwise
  silent at run time. Do not add an opt-out flag to `BuildingBlocksOptions`; if a check is too
  strict, fix the check.
- **Assume at-least-once delivery everywhere.** Projection handlers must be idempotent and
  order-aware via the aggregate `Version` watermark; never write a consumer that assumes exactly-once.

## Testing & CI

Full strategy: `docs/architecture/testing-strategy.md`. What it will not tell you from the code:

- Test projects mirror source structure 1:1. Domain tests use hand-written test doubles
  (`TestDoubles/`), not mocks — the domain has no infrastructure to mock. NSubstitute is for
  application/persistence/messaging tests.
- Assert observable behavior, not internals ("creating a recipe raises a `RecipeCreated` event").
- Add or extend tests alongside **any** behavioral change, and make them pass.
- Container-backed tests guard with `Skip`/`Assert.SkipUnless`, but CI sets
  `VITALSYNC_REQUIRE_CONTAINERS=1` and `VITALSYNC_REQUIRE_SMOKE=1` so they **fail instead of
  skipping**. Keep both flags when touching `.github/workflows/build.yml`, and keep
  `--report-xunit-trx` together with its `if: failure()` reporting step — either alone is useless.
- Fixture types that must live outside the test assembly go under
  `BuildingBlocks/tests/ExternalAssemblies/<ShortName>Fixture/`; keep names **short** (Windows
  `MAX_PATH`) and add the project to `VitalSync.slnx`.

## When contributing

1. Reusable and VitalSync-agnostic → `BuildingBlocks`; domain logic → the matching service.
2. Respect the non-negotiable rules above and read the sources from the table before editing.
3. Add or update tests, and make sure they pass.
4. If a change affects architecture, add or update an ADR (template in `decisions/README.md`).
5. Check the `*.md` files your change affects and update them — including this file, whenever you
   find a gap or an ambiguity in the guidance here.
6. Match existing style; respect `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`.
7. **Always work on `main`** — never a separate branch, and never ask which branch to use.
8. Use short, clear commit messages, but **never commit yourself**.
9. If you are unsure about a decision, **ask a human** — Copilot is not the arbiter of architecture
   or domain rules.
