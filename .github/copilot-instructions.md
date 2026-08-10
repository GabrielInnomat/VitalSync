# Copilot instructions

This file provides guidance to Copilot (chat and coding agent) when working with code in this repository.

## What VitalSync is

A cloud-native, distributed platform unifying **nutrition**, **fitness**, and
**health analytics** behind a single Blazor UI. Built as independent ASP.NET Core
microservices using **DDD**, **CQRS**, and **selective Event Sourcing**.

> Core principle: **the architecture is fixed, the domain is fluid.**
> Technical/architectural decisions are mandatory. Business/domain details are
> refined iteratively. When a change affects architecture, add or update an ADR.

## Build, test, run

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~AggregateRootTests"
dotnet test BuildingBlocks/tests/BuildingBlocks.Domain.Tests
dotnet run --project src/Aspire/VitalSync.AppHost
```

Prerequisites: the .NET SDK pinned in `global.json` (`10.0.302`, `rollForward: latestFeature`) and Docker (for messaging/database infrastructure). **No Aspire workload** — the AppHosts reference `Aspire.AppHost.Sdk` as a package, and CI builds green without one.

Global build settings (`Directory.Build.props`) apply solution-wide: nullable + implicit usings enabled, `latest-all` analysis level, and **warnings treated as errors**. Respect `.editorconfig` at each level: the root one, plus exactly **two** project-level ones under `BuildingBlocks/src` — `BuildingBlocks.Domain` relaxes `CA1033` (`IDomainEventOwner`/`IStateOwner` are implemented *explicitly* on purpose, ADR-0007) and `BuildingBlocks.Application` relaxes `CA1000` (`Result<T>` needs static factories) — plus one for the test projects. **`BuildingBlocks.Infrastructure` has none and needs none**: it carries the full analyzer set unrelaxed, and it should stay that way — a new suppression there is a smell worth arguing about first. Test-only analyzer relaxations that no `.editorconfig` covers live in the test `.csproj` as `NoWarn`.

Package versions are managed centrally in `Directory.Packages.props` (`ManagePackageVersionsCentrally` plus `CentralPackageTransitivePinningEnabled`): a `.csproj` carries `<PackageReference Include="..." />` with **no `Version`** attribute, and a new package needs a `<PackageVersion>` entry there first — otherwise restore fails (NU1010) instead of silently drifting. Do not reintroduce a per-project `Version`; that is exactly the drift the file removes (`NSubstitute` once sat at 5.3.0 in one test project and 6.0.0 in two others). Two version numbers stay outside it because NuGet cannot manage them: the SDK pin in `global.json` and `Aspire.AppHost.Sdk` in each AppHost's `<Project Sdk="...">` attribute.

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
    └── VitalSync.ServiceDefaults.Tests/
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
  (`tests/VitalSync.ServiceDefaults.Tests`). `AddServiceDefaults()` also registers the
  OpenTelemetry sources `BuildingBlocks`, `Npgsql`, `Wolverine`, and `Marten` (plus the
  `Npgsql` meter), so CQRS, database and transport spans exist without any per-host wiring —
  do not re-add them.

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
  domain events per ADR-0007 — `IHasDomainEvents` (public) vs. **`IDomainEventOwner`**
  (clear; explicit, infrastructure-only). The interface was renamed from
  `IDomainEventsManager` by the ADR-0007 naming amendment; `*Owner` is the house
  signal for "privileged view, implemented explicitly, infrastructure only", shared
  with `IStateOwner`. The two are deliberately **not** merged: `IDomainEventOwner`
  applies to every aggregate, `IStateOwner` only to state-stored ones.
- **Entity identity and equality** follow ADR-0008. Two rules from its 2026-08-05 amendment:
  a key is constrained `where TKey : struct, IEntityKey, IEquatable<TKey>` — without the second
  interface `Id.Equals` binds to `ValueType.Equals(object)` (reflection plus a boxing allocation
  per comparison) and a key with no value equality compiles happily; the constraint is viral and
  repeated at all 13 declarations carrying a `TKey`, and `EntityKeyConstraintTests` catches a new
  one that forgets it. And `EntityBase.Equals` deliberately does **not** special-case an empty
  identity: four guards (`Entity`'s constructor, `AggregateRoot.ApplyEvent`, `IStateOwner.Restore`,
  both repositories' `AddAsync`) already make an unidentified entity unreachable from outside the
  domain, whereas an `!Id.IsEmpty && …` clause would make an object unequal to itself and need a
  `ReferenceEquals` short-circuit to stay reflexive. Do not "fix" the equality rule; add the guard.
  Corollary worth knowing: a hull's hash code **changes** when it gains identity, so never put one
  in a `HashSet` or use it as a dictionary key.
- **Event identity is asymmetric** (ADR-0029): domain events are **pure value
  records with no identity fields** — `IDomainEvent`/`DomainEvent` are empty, and
  `EventId`/`OccurredAt` are minted by the unit of work at commit and travel on the
  `DomainEventEnvelope`. Integration events **carry `EventId`/`OccurredAt` on the
  event** (required by `IIntegrationEvent`) because no envelope crosses the context
  boundary; mappers populate both from the `DomainEventMetadata` they receive —
  **never** a fresh Guid per invocation, or redeliveries break deduplication. Do not
  "clean up" this asymmetry.
- **Persisted names are declared, never derived** (ADR-0030): every domain event needs
  `[EventName("widget-created-v1")]` and every aggregate needs `[AggregateName("widget")]`,
  both lower-case kebab-case. The event name is what the outbox row and `mt_events.type`
  store; the aggregate name prefixes the event stream and travels on every envelope. A CLR
  rename must never touch persisted data, so the class name is not a contract. A host names
  the declaring assembly via `options.AddDomainEventsFrom(assembly)` — configuring persistence
  without it throws at startup, and a missing or duplicated `[EventName]` throws at
  registration. `Type.GetType` over stored data is gone; the readable type set is closed.
  There is **one** kebab-case validator, `BuildingBlocks.Domain.Naming.KebabCase`, used by both the
  persisted-name attributes and `[IntegrationEventTopic]`. It is public purely because
  `Infrastructure` needs it; never write a second copy of the character loop.
- **The state is an abstract record, not an interface** (ADR-0030, amending ADR-0010): a state
  derives from `AggregateState<TSelf, TKey>` and writes only its fields, `Empty` and
  `override Apply`. The base carries `Id` and `Version`; because a record's copy constructor is
  virtual, `this with { … }` in the base returns the derived type, so the base owns the version
  bookkeeping through an `internal WithVersion` that domain code can neither reach nor
  implement wrongly. A state record therefore has **no** version boilerplate and there is no
  guard to write — the cost is one unchecked cast, once, in Building Blocks.
- **The aggregate version lives on the state** (ADR-0030): `AggregateRoot` advances it on every
  folded event, including one the state's `Apply` ignores. One number serves three purposes —
  Marten's expected stream version, the EF `IsConcurrencyToken()` column that finally gives
  state-stored contexts optimistic concurrency, and the per-aggregate sequence on
  `DomainEventEnvelope`/`DomainEventMetadata` (`AggregateName`, `AggregateId`, `Version`) that
  projections use as an order watermark. It has **exactly one source**: the explicitly
  implemented `IStateOwner.Version`. `IEventSourcedAggregateRoot<TKey>` declares
  `LoadFromHistory` and nothing else (ADR-0030 amendment 2026-08-06) — read a version with
  `((IStateOwner)aggregate).Version` on both persistence paths.
- **Business rules and domain validation** follow ADR-0009, and a `null` rule **throws**
  (amendment 2026-08-06). `RuleChecker` used to evaluate `rule?.IsBroken() == true`, so the
  validation layer fell silent in exactly the case it exists for: a factory returning `null`
  meant "rule passed". All four overloads guard with `ArgumentNullException.ThrowIfNull`.
- **The `params` overloads collect, they do not short-circuit** (ADR-0009 amendment 2026-08-08).
  Every rule is evaluated, the broken ones are gathered and **one** exception is thrown at the
  end carrying `IReadOnlyList<RuleViolation> Violations` (`Code`, `Target`, `Message`). The
  array's `null` guard therefore runs **before** the first rule, or whether a `null` surfaces as
  an `ArgumentNullException` or vanishes behind collected domain errors would depend on its
  position. Consequence for rule authors: **rules in one call must be independent** — where one
  rule only makes sense after another passed, write two consecutive `Check` calls, which is what
  `Gadget` already does. **Both** rule interfaces carry a `Code` — it identifies the *rule*, so a
  client can react to that constraint rather than to "some business rule" — and only
  `IDomainValidationRule` adds `string? Target` (the field name; `null` when the rule spans
  several fields), because an invariant is a statement about the aggregate, not about a field.
  `RuleViolation.Code` is therefore **not** nullable; the message-only constructors CA1032 forces
  on both exceptions carry no rule and fill in the exception's own `public const FallbackCode`
  (`domain.validation` / `domain.business_rule`), which
  `ExceptionToResultBehavior.ValidationFailureCode`/`BusinessRuleFailureCode` merely alias. The
  two kinds are never merged and never mixed in one call, so a handler invocation raises at most
  one exception and **every `Failure` in a failed `Result` shares one category**.
- Aggregates use an **aggregate state object** (ADR-0010).
- **One aggregate authoring model** — every aggregate derives from the state-fold
  base `AggregateRoot<TKey, TState>` and mutates only via `RaiseEvent`; the
  **event-sourced base is additive** (`LoadFromHistory` only), per
  ADR-0025 (which supersedes ADR-0012). Only apply ES where the event history
  carries business value.
- **EF Core maps the aggregate's _state_, never the aggregate** (ADR-0025 amendment):
  `Id => State.Id` is computed, so it cannot serve as a mapped primary key, and a
  positional record also fails as a `ComplexProperty`. The state record maps as an
  ordinary **entity type** — one table, one id column, no shadow key. Infrastructure
  reaches it through `IStateOwner`, implemented **explicitly** on `AggregateRoot` so
  domain code never sees it and cannot bypass the event fold.
- **Reconstitution, not construction** (ADR-0025 amendments 2026-08-03/2026-08-04): every
  aggregate keeps its parameterless constructor **private** — the aggregate's named
  factory stays the only public way in (`new Widget()` is `CS1729`). Repositories obtain
  the empty hull through the internal, per-type-cached `AggregateFactory` in
  `Infrastructure`; the former `IReconstitutable<TSelf>` interface with its explicit
  `CreateEmpty` per aggregate is **deleted** — the per-aggregate ceremony outweighed the
  compile-time proof. Instead the convention is validated **at host startup**:
  `AddBuildingBlocks` scans the `AddDomainEventsFrom` assemblies and fails registration,
  naming the aggregate, when a parameterless constructor is missing (same fail-fast bar
  as `[EventName]`/`[AggregateName]`, ADR-0030). Both persistence paths are identical.
  New aggregate? Private parameterless ctor, or the samples' `AggregateConventionTests`
  scan and host startup fail.
- **A child entity raises through its root** (ADR-0032): a child with invariants of its own is
  `Entity<TKey, TState>` over an `EntityState<TSelf, TKey>` — the child counterpart of
  `AggregateState`, **without** a version. The child's state lives in the root's state (ADR-0031),
  so `State.Apply` folds it, usually by delegating to the child state's own `Apply`; there is no
  second routing step. The hull reads its state **through the root** via `GetCurrentState()` (a
  method, not a property, because it throws once the child was removed) and calls
  `RaiseEvent`, which the root receives through the explicitly implemented `IDomainEventRaiser`.
  So: **one event list, one order, one version — all on the root**, and a child-only change still
  advances that version. Never give a child its own event list and never record events in a state
  (`Apply` stays pure): record equality, append ordering and snapshot safety all depend on it.
  A root exposes children as hulls it builds on demand (`widget.Part(id)`) with an `internal`
  constructor; it keeps the state lookup (`FindPart`) to itself. **Every entity has a state**, so
  `Entity<TKey, TState>` is the only non-aggregate entity base — the state-less `Entity<TKey>` is
  gone (ADR-0008/0025 amendments) and `EntityBase<TKey>` has exactly two children, this one and
  `AggregateRoot<TKey, TState>`.
- **One repository contract**: `IRepository<TAggregate, TKey>` with `GetByIdAsync`
  and `AddAsync` only (ADR-0026) — no `Remove` (removal is a soft-delete state
  change), no `Save`/`Update` (retrieved aggregates are tracked; changes flow
  through the unit of work). Both EF Core and Marten implement the same contract, and
  **both track in the repository**: EF's change tracker only ever sees states, so
  `EfCoreAggregateTracker` mirrors `MartenAggregateTracker` (ADR-0026 amendment).
  Consequence to know: **domain events are collected only from aggregates that went
  through `IRepository`** — an entity written straight into the `DbContext` produces
  none.

- **Folder = namespace in `Domain` and `Application`, and the namespaces are contract.**
  `Domain` is cut into `Aggregates/`, `Entities/`, `Events/`, `Naming/`, `Rules/` (`IClock`
  stays in the root); `Application` into `Cqrs/`, `Results/`, `Persistence/`, `DomainEvents/`,
  `IntegrationEvents/`, `ReadModels/` (root empty). Domain and integration events are **deliberately not**
  one `Events/` folder — that line is the bounded-context boundary. Unlike `Infrastructure`,
  where nearly everything is `internal` and the namespaces are invisible, every type here is
  `public`: moving a file changes each exported type's `FullName` and breaks every consumer,
  so `PublicSurfaceTests` in both test projects pins the complete exported-type list. Add,
  move or rename a public type and that test fails until the list is updated — deliberately.
  A service does **not** repeat the usings per file: it declares them once as
  `<Using Include="BuildingBlocks.Domain.Aggregates" />` etc. in its `.csproj`, the way all
  four sample Domain/Application projects do (`Widget.cs` has no using directive at all).
## Application / CQRS conventions (from accepted ADRs)

- CQRS abstractions and the `Result` / `Failure` model live in
  **`BuildingBlocks.Application`** (depends only on `Domain`). A **hand-rolled
  dispatcher** is used instead of MediatR (ADR-0015); the DI-based implementation
  lives in `BuildingBlocks.Infrastructure`.
- Handlers and dispatch are **async-only** with a `CancellationToken`; no sync overloads,
  and every awaitable contract method **carries the `Async` suffix** — `ISender.SendAsync`,
  `ICommandHandler`/`IQueryHandler`/`IProjectionHandler`/`IPipelineBehavior.HandleAsync`,
  alongside the already-suffixed `CommitAsync`/`GetByIdAsync` (ADR-0015 amendment 2026-08-06).
  **One deliberate exception:** a method that satisfies **Wolverine's** discovery convention
  rather than one of our contracts keeps the name Wolverine expects — today
  `DomainEventEnvelopeHandler.Handle` and the samples' `WidgetCreatedConsumer.Handle`.
  Neither implements a Building Blocks interface, so the compiler cannot catch a rename
  there: Wolverine would silently stop discovering the handler and the message would be
  dropped with no route and no error. Only rename methods that implement one of our contracts.
- **Handler registration** via `BuildingBlocksOptions.AddHandlersFrom(assembly)` is
  idempotent for multi-handler contracts (`IProjectionHandler<>`,
  `IIntegrationEventMapper<>`) and enforces **exactly one** handler per command/query —
  two different handlers for the same `ICommand`/`IQuery` throw at registration, not
  at request time. **Startup handler validation is on by default**: a hosted service
  registered by `AddBuildingBlocks` verifies at host start that every command/query
  in the scanned assemblies resolves to a handler (fail-fast instead of
  "no service registered" on the first request) and rejects request types that
  implement **more than one** `ICommand<>`/`IQuery<>` contract — a command or query
  has exactly one result type.
- **The start-up checks are not optional** (ADR-0027 amendment 2026-08-05). The former
  `ValidateHandlersOnStart` and `ValidateWolverineOnStart` switches are **deleted**, and no
  new check gets one. Every one of these checks exists because the failure it catches is
  otherwise silent at run time; an opt-out restores exactly that silence, and the only host
  that would reach for it is the one already in trouble. Do not add an "escape hatch" flag to
  `BuildingBlocksOptions` — if a check is too strict, fix the check.
- **Commands** return `Result` or `Result<T>` (a **create** returns the new typed id,
  e.g. `Result<RecipeId>`; **delete/void** returns `Result`). **Queries** return `Result<T>`.
- Expected domain errors (`BusinessRuleViolationException`, `DomainValidationException`)
  are **translated to `Result.Failed`** by an Application pipeline behavior — **one `Failure`
  per `RuleViolation`**, so a collected multi-field error arrives intact (ADR-0017 amendment
  2026-08-08). `Failure` carries an optional `Target` (the field name), a behavior returns
  several at once via `RequestPipeline<TResponse>.Failed(IReadOnlyList<Failure>)`, and each
  gRPC adapter writes them all into the **trailers** (`failure-count`,
  `failure-{i}-code`/`-message`/`-target`) while the status still comes from the shared
  category. Unexpected
  errors bubble to a thin global handler (ADR-0017). `FailureCategory` is one of
  `Validation`, `BusinessRule`, `NotFound`, `Conflict`, `Forbidden` — transport status
  mapping is owned by the BFF/service host, never by `Application`. There is
  deliberately **no** `Unexpected` value (ADR-0017 rejects it explicitly: an unexpected
  error stays an exception rather than becoming a second failure channel) and no
  `Unauthorized` (authentication never reaches this layer). Adding a category is **not**
  compiler-checked at the transport boundary and cannot be — a `switch` over an enum
  always needs a discard arm (CS8509), which swallows the new value. Two run-time
  guards stand in for it: each sample's `FailureStatusMappingTests` walks
  `Enum.GetValues<FailureCategory>()` and fails on anything reaching the adapter's
  fallback status, and `FailureTests` requires a factory of the same name on `Failure`.
- Pipeline behaviors run in an **explicit numeric order**: logging is outermost so
  expected domain errors are logged as `Warning` (not `Error`), then exception-to-`Result`
  translation, then the unit of work closest to the handler. Built-ins occupy fixed
  slots; services add their own via `BuildingBlocksOptions.AddPipelineBehavior(type, order)`
  (negative runs before built-ins, higher runs after). That is the **only** way to add a
  behavior: one registered directly on the `IServiceCollection` has no order and fails
  `AddBuildingBlocks` (ADR-0027 amendment 2026-08-05) — an unordered behavior would run at
  order 0, silently sharing the logging behavior's slot.
- **A behavior gets a `RequestPipeline<TResponse>`, not a bare continuation** (ADR-0015 amendment
  2026-08-05). `pipeline.NextAsync(ct)` runs the rest of the chain; `pipeline.Failed(failure)`
  builds a failed response **of the behavior's own `TResponse`** from a factory the dispatcher
  supplied, because only the dispatcher still knows whether that is `Result` or `Result<T>`. This
  is why a short-circuiting behavior needs neither a generic constraint nor reflection — the
  reflection-based `FailureResults` is deleted. The factory is `Result.Failed(...)` /
  `Result<T>.Failed(...)`: `Failure` names the error **value**, never the factory
  (ADR-0017 amendment 2026-08-05). Since the 2026-08-08 amendment the factory takes an
  `IReadOnlyList<Failure>` and `Failed` has both overloads, so a behavior can short-circuit with
  several failures at once.
- **`AddBuildingBlocks` is called exactly once per host** and a second call **throws**
  (ADR-0027 amendment 2026-08-05). The `PipelineBehaviorRegistry`, the
  `WolverineWiringSettings` and the `DomainEventTypeRegistry` are one shared instance each;
  a second call used to fill fresh ones nobody resolves, so its behaviors ran at order 0,
  its persistence/messaging selection was ignored and its `[EventName]` names were missing
  at the first commit. Every selection goes into the same options callback.
- **Committing nothing is a choice, not a default** (ADR-0027 amendment 2026-08-05).
  `UnitOfWorkBehavior` takes a **non-optional** `IUnitOfWork`; Building Blocks registers a
  `NullUnitOfWork` fallback (`TryAddScoped`, a real one always wins) so the pipeline resolves,
  and `UnitOfWorkPresenceCheck` **throws at start** — naming the commands — when the scanned
  assemblies contain commands, no persistence strategy was selected and the host registered no
  `IUnitOfWork`. A host that genuinely commits nothing says `options.UseNoPersistence()`: a
  positive selection on `PersistenceChoice`, mutually exclusive with the two persistence
  strategies, not an opt-out flag. Never restore the old `IUnitOfWork? = null` default — its
  failure mode is "command reports success, data is gone".

## Infrastructure package layout

- **`internal` is the default in `BuildingBlocks.Infrastructure`.** A host consumes it
  through DI, so an implementation registered in the container has no reason to be
  visible. Exactly four types are public API — `ServiceCollectionExtensions`,
  `HostApplicationBuilderExtensions`, `BuildingBlocksOptions`,
  `EntityKeyModelBuilderExtensions` — plus `InfrastructureProvisioning`, public because it
  is an argument a host passes (ADR-0037), `PersistedSchema`, public because a service's
  **tests** call it (ADR-0035), and the two read-model rebuild runners
  (`StateStoredReadModelRebuildRunner<TContext>`, `EventSourcedReadModelRebuildRunner`), public because a
  migration worker constructs one without the full wiring (ADR-0036). Seven more are public **only** because Wolverine
  generates C# into another assembly and names them (`DomainEventEnvelope`,
  `DomainEventEnvelopeHandler`, `DomainEventEnvelopeSerializer`, `DomainEventTypeRegistry`,
  `IIntegrationEventSinkFactory`, `IntegrationEventSourceContext`,
  `OwnContextIntegrationEventFilter`). `PublicSurfaceTests` pins both lists — a new
  `public` type fails it until it is listed with a reason.
- **Folder = namespace, and the cut carries meaning.** `Persistence/StateStored/` and
  `Persistence/EventSourced/` are the two mutually exclusive write paths; the shared
  `Persistence/` parent holds only what both need — including `EntityKeyValueConverter`,
  because an event-sourced context still uses EF Core for its **read** models, and the
  tracking base plus the envelope factory, because both write paths mint identity the same
  way. Configuring
  Wolverine is not messaging: it happens once at composition time and lives under
  `DependencyInjection/Wiring/`, while `Messaging/DomainEvents/` (publisher, projection
  runner, envelope) and
  `Messaging/IntegrationEvents/` hold what runs per message. Start-up checks live in
  `DependencyInjection/Validation/`. **Nothing under `Persistence.*` is public** —
  `PublicSurfaceTests.NoInfrastructureImplementationIsPublic` fails on it — so a type the
  host itself constructs lives outside that tree even when it is persistence-adjacent:
  `PersistedSchema` in `Schema/`, both rebuild runners in `ReadModels/`.
- **`ApplyEntityKeyConversions` converts, it never discovers** (ADR-0033). EF Core's discovery
  never finds an `IEntityKey<T>` property ("not a supported primitive type"), and the helper used
  to compensate with a CLR scan plus `AddProperty` — a helper that wrote to the model and could
  therefore contradict it (an `Ignore()`d key returned as a column; a computed get-only key broke
  model creation). It now only walks `GetProperties()` and attaches the converter, skipping
  properties that already have one. So **every `DbContext` maps every typed key explicitly**,
  which every context here already does anyway for column names, `IsRequired` and
  `IsConcurrencyToken`. Forget one and EF Core fails at model build, naming the property and both
  remedies — loud, not silent. Owned types were never affected (separate entity types, configured
  via `OwnsMany`). Complex types stay out of scope: no `ComplexProperty` exists in the repo.
- **A typed key serializes as its bare value** (ADR-0034). `IsEmpty` is a computed domain
  predicate, but to a serializer it is an ordinary property, so a key used to reach three
  append-only or contractual stores as `{"Value":"8f3a…","IsEmpty":false}` — the Marten event
  stream, the outbox payload and the integration-event body. `EntityKeyJsonConverterFactory`
  now writes the bare value (`"GadgetId": "8f3a…"`) and reads it back through the same
  single-argument constructor the EF Core value converter needs (shared
  `EntityKeyActivator<TKey, TValue>`). `EntityKeyJsonOptions` is the **one** place that attaches
  it, applied at all three sites — including Marten, which therefore runs on System.Text.Json:
  a `[JsonIgnore]` binds to one serializer and Marten's default was the other, so it would have
  been silently ineffective exactly where the immutable data lives. The old object shape is
  **not** accepted on read; do not add a tolerance branch, that would make two formats permanent.
- **A persisted field name is pinned by a snapshot, not by an attribute** (ADR-0035). ADR-0030
  killed derived names at the type level; the field level stayed derived, so renaming a property
  renames the JSON field and stored events deserialize to `default` — silently. There is still
  **no `[JsonPropertyName]` per field**: a field rename almost always changes meaning, so it
  *should* cost a new event version. Instead each service's tests call
  `PersistedSchema.Verify(path, assemblies)` against a checked-in `EventSchema.approved.txt`
  next to the test (both samples have one). The renderer reads through **`JsonTypeInfo`**, so it
  pins what the serializer actually does — a `[JsonPropertyName]` shows up, a typed key renders
  as its bare value (ADR-0034). Decision rule, also carried in the failure message: a field only
  **added** stays readable → approve the new snapshot; renamed, removed or retyped → leave the
  event alone and add a successor under a new `[EventName]`. It is a **test**, not a start-up
  check, because a baseline does not exist at run time. The state-stored path needs no snapshot
  (EF migrations are its baseline) but obeys the same rule by force: `AggregateStateModelCheck`
  rejects at start-up any state or owned-child property without an explicit `HasColumnName`, or
  without `HasJsonPropertyName` for a `ToJson()` child. Two limits to know: the snapshot pins
  **names, not wire formats** (adding a `JsonStringEnumConverter` later is invisible), and read
  models are deliberately out (derived and rebuildable). Marten snapshotting would move an
  event-sourced **state** into durable JSON — it then belongs in the baseline.
- **An event's identity is minted in exactly one place.** `DomainEventEnvelopeFactory`
  reads `IClock.Now` **once per commit** and counts each event's per-aggregate `Version`
  backwards from the aggregate's current version; both units of work call it and contain
  no envelope arithmetic of their own. Duplicating that block per persistence path is how
  ADR-0029 and ADR-0030 drift apart silently — a wrong `Version` breaks the projection
  watermark, and no projection test asserts it. The same applies one level down:
  `AggregateTracker<TEntry>` owns tracking, validation and event clearing, and a subclass
  adds only what its store must remember about an entry.
- **The persistence selection is one value, not flags.** `PersistenceChoice` is a closed
  hierarchy — `None`, `Marten`, `EfCore(connectionString)` — with private subtypes.
  "Route domain events" and "a message store exists" are the same fact (`IsSelected`),
  and the EF Core write connection string has exactly one home, which is why the outbox
  cannot end up in a different database than the aggregates. `WolverineWiringSettings`
  has no public setters: selecting is a method, and it throws both when two different
  strategies are chosen and when the same strategy is chosen twice with different
  arguments (a bounded context has one write database, ADR-0021). An identical repeated
  call stays legal. Do not add a derived flag next to the choice — derive it from the
  choice.
- **Never name a folder after a vendor whose namespace you use.** A
  `Persistence/Marten/` folder would break every `using Marten;` in it, because C#
  resolves against the enclosing namespaces first. Hence `StateStored`/`EventSourced`
  and `Wiring` — which describe the role and are the better names anyway. The same goes
  for **type** names a vendor already uses for something else: the dispatcher is
  `RequestSender` and the publication step is `DomainEventPublisher`, because Wolverine
  means a transport endpoint by `ISender` and "putting a message on the broker" by
  *publish*. Qualify a bare noun with what it actually operates on. The rule covers our
  own types too: the routing-key helper is `TopicResolver`, never `IntegrationEventTopic`,
  which is the attribute it reads.
- **`BuildingBlocksOptions` is a fluent facade, not a worker.** Each method validates
  its arguments and delegates to one collaborator in `DependencyInjection/Registration/`
  — `HandlerRegistrar` (scanning, handlers, behaviors), `PersistenceRegistrar` (EF Core
  and Marten), `MessagingRegistrar` (RabbitMQ selection and subscription),
  `DomainEventCatalog` (domain-event assemblies and the registry they freeze into).
  The options type references no third-party persistence or transport package at all,
  so each of those concerns has exactly one file. New selection method? Add it to the
  matching registrar and keep the facade method a delegation.
- **The composition root is phased.** `ServiceCollectionExtensions` is a facade; the
  internal `BuildingBlocksComposition` runs `Configure` → `Validate` → `RegisterCore` →
  `RegisterStartupChecks`. The order is load-bearing: `Validate` materialises the
  `DomainEventTypeRegistry` and thereby freezes `AddDomainEventsFrom`. Add new
  registration work inside the matching phase, not at the end of the method.
- **A start-up check is an `IStartupCheck`, never its own hosted service.** One
  `StartupCheckRunner` drives them all: `BeforeHostedServicesStart` checks run in its
  `StartAsync`, `AfterHostedServicesStarted` checks in its `StartedAsync`. The contract is
  `Task RunAsync(CancellationToken)`; a check that does no I/O derives from
  `SynchronousStartupCheck` and overrides `void Run()` instead — six bodies returning
  `Task.CompletedTask` trip `IDE0046` and hide which checks actually wait on something.
  Only the
  **phase** matters — the checks are pure readers, so their relative order decides only
  which message a broken host sees first, and the .NET host's three-pass start
  guarantees every `StartAsync` finishes before any `StartedAsync` begins. That is why
  `IntegrationEventSubscriptionCheck` can read Wolverine's handler graph without
  depending on a registration index. A check **registers unconditionally and guards
  itself** (early return when its capability was not selected) rather than being
  registered conditionally, and it probes the **built container**
  (`IServiceProviderIsService`), not the `IServiceCollection`. The one exception to
  "pure reader" is `MartenSchemaProvisioner` (ADR-0037), which writes; it is the **only**
  writer and a second one would need a real ordering mechanism instead.
- **Creating infrastructure is a role, not a start-up side effect** (ADR-0037).
  `options.ProvisionInfrastructure(InfrastructureProvisioning.AtStartup)` is selected by
  **exactly one host per bounded context** — the MigrationService worker, through the same
  context extension method its service uses, so the two cannot disagree about a connection
  string, a context name or an event assembly. Every other host keeps the default `Never`:
  Marten runs `AutoCreate.None`, Wolverine's `AutoBuildMessageStorageOnStartup` is `None`,
  the RabbitMQ topology is not declared at all, and two checks fail the start instead:
  `InfrastructurePresenceCheck` (`AfterHostedServicesStarted`) for the message store's
  tables, `BrokerTopologyCheck` (`BeforeHostedServicesStart`) for the exchange and the
  subscriber queue. One value, not three flags — the only sensible combinations are all-on
  and all-off. Two traps: `AddResourceSetupOnStartup` is **deliberately not used** (it opens
  RabbitMQ channels concurrently with Wolverine's own start and hits a null-channel
  dereference in `RabbitMqListener.CreateAsync`), and **`DeclarePassive` does nothing** —
  Wolverine reads it only inside the `DeclareAsync` that `AutoProvision` guards, so setting
  it read like a guarantee while a missing exchange let the host start and every publish
  return successfully into the void. Hence a check of ours, pinned against a real broker by
  `BrokerTopologyCheckTests`. The check asks only whether the exchange and the queue **exist**; two
  properties one level below are pinned by tests instead. A queue that exists but is not **bound** loses every message just as silently, and no
  start-up check can ask AMQP about a binding without creating it (`queue.bind` provisions), so
  `BrokerTopologyCheckTests` asserts it with a raw publish and a message count. And the platform
  exchange must be **topic**: nothing sets the type — `DeclareExchange` carries only
  `IsDurable` and setting the type there has no effect, because the topic-routing overload of
  `PublishMessagesToRabbitMqExchange` wins. Swap that rule for a plain
  `PublishAllMessages().ToRabbitExchange(...)` and the exchange silently becomes a fanout, which
  makes every binding a catch-all; that mutation takes 11 tests red today. An integration test
  that owns its own container **is** that container's provisioning host and must select
  `AtStartup`.

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
  EF Core finds nothing to save and the change is silently lost. The repository takes an
  internal `WriteDbContextAccessor`, **never** a bare `DbContext`: a bounded context owns a
  write *and* a read database (ADR-0021), so the unqualified `DbContext` key belonged to the
  write context by convention only, and a host registering its read context under that key
  decided by registration order which database was written to — silently. The accessor is
  filled by `UseEfCorePersistence<TContext>` and used by nobody else. Do not reintroduce a
  `DbContext` registration, and do not replace the accessor with a marker interface: that
  would put a requirement on every service's own context type for a problem that lives
  entirely in our registration.
- **A child of an aggregate maps as an owned type** (ADR-0031): `OwnsMany(...)` with its own
  table and its own strongly typed key via `HasKey`; `ToJson()` only for identity-less values.
  `CurrentValues` covers scalars, so `EfCoreUnitOfWork` hands the state to
  `AggregateStateGraph.Reconcile`, which copies the scalars **and** reconciles the **owned graph**:
  matched children get `CurrentValues.SetValues` and are
  recursed into, new ones are added to the tracked collection, vanished ones removed — matched by
  **key**, at any depth, yielding `UPDATE`/`INSERT`/`DELETE` with stable row identity. `FindAsync`
  loads owned children with their owner (no `Include` needed). Do **not** "simplify" this to
  assigning the collection to the navigation: that works one level deep and then throws, because the
  grandchildren carried along collide with the tracked ones under the same key. A `ToJson()`
  collection deliberately stays on the assignment path (one column, shadow key). Three guards make
  the rule non-optional: a navigation from an `AggregateState` to an **independent** entity type is
  rejected at host startup, an owned non-JSON collection without a single non-shadow key is rejected
  at host startup, and a read-only, fixed-size or `null` collection throws a
  `NotSupportedException`. Authoring a state with children: the collection is a `{ get; init; }`
  property, **never** a positional record parameter (EF Core then finds no suitable constructor),
  and it is built with `ToList()` — a collection expression assigned to `IReadOnlyCollection<T>`
  compiles to a read-only array, not a `List<T>`. Referencing another aggregate from a state is
  impossible by design; hold its typed id as a scalar instead.
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
  **must be idempotent and per-aggregate order-aware** — `IProjectionHandler.Handle` receives
  the `DomainEventMetadata`, so a handler keeps the last processed `Version` per aggregate on
  its read model and ignores anything at or below that watermark (ADR-0030); an event below the
  watermark is dropped, not field-merged. Reads are **eventually consistent** with writes.
- **Projection and integration-event publication are two queues, not one handler** (ADR-0022
  amendment 2026-08-10). `DomainEventEnvelope` carries only the integration-event step; its
  handler publishes and then forwards a `ProjectionEnvelope` to a second local queue that runs
  the projections. Both carry `UseDurableInbox()`, both are partitioned per aggregate and both start from the same
  outbox flush, so per-aggregate order, crash safety and "nothing escapes an uncommitted write"
  are unchanged. The order is deliberate — the **non-recoverable** step goes first: a read model
  can be rebuilt (ADR-0036), an integration event that was never sent cannot. Hence the three
  commitments pinned by `DispatchIsolationTests`: an uncommitted command produces neither; a
  failing projection does **not** stop the integration event; a failing mapper **does** stop the
  projection, because it fails before the forward. Never merge the two back into one handler.
- **The domain-event queues are partitioned per aggregate, not serialised globally** (ADR-0022
  amendment 2026-08-11). Both local queues use `PartitionProcessingByGroupId(PartitionSlots.Five)`
  with the group id `"{AggregateName}/{AggregateId}"`, supplied by
  `options.MessagePartitioning.ByMessage<...>`. The former `.Sequential()` bought a
  **per-aggregate** promise by serialising **every** event of the whole service. Three properties
  are pinned by `DomainEventPartitioningBehaviourTests` against a real message store: within one
  group a message in a retry cooldown is **not** overtaken (the slot blocks for the cooldown —
  measured, not assumed; had it moved on, a later event would advance the read-model watermark past
  the waiting one and the successful retry would be discarded), across groups messages run
  concurrently, and the wiring itself is partitioned. That last test **must** boot a host: Wolverine
  applies listener configuration during bootstrapping, so `GroupShardingSlotNumber` is still
  `null` on a merely configured `WolverineOptions` and a check there would pass no matter what.
  The slot count is a **constant, never a setting** — changing it remaps aggregates onto slots, so
  two processes running different counts handle one aggregate at the same time and a rolling restart
  reorders events silently; changing it requires a drain, which is exactly what a config knob invites
  an operator to skip. And the guarantee is per **process**, exactly as it was before: with more than
  one instance of a context, per-aggregate order across hosts is not guaranteed. That gap is
  pre-existing and orthogonal — partitioning neither widens nor closes it — and it is unreachable
  in a single process, because the cooldown blocks its slot. ADR-0022 therefore names a **gap
  detection as a precondition of running a second instance**: the watermark discards rather than
  merges, and projections write increments, so a dropped event is permanently wrong. The detection
  belongs in the dispatch path (`ProjectionRunner`), never in the read-model watermark — a domain
  event with no projection handler produces a legitimate version gap and would raise false alarms.
- **Read models are owned by each service, not a Building Block** — the service owns
  its read-model schema, projection handlers, and queries; Infrastructure ships only
  the plumbing (Publisher, outbox, dispatch loop, projection runner, transport). Read
  models are **derived and rebuildable**, but by different means per path (ADR-0036).
- **A state-stored read model is rebuilt from the current state, not from a replay**
  (ADR-0036). An event-sourced context replays its Marten stream; a state-stored context
  has no surviving history — its outbox row is deleted once delivered — so it derives the
  read model again from the **current aggregate state**. The live event-based path is
  **unchanged**; the rebuild is a second, explicitly invoked path:
  `IReadModelRebuilder<TAggregate, TKey>` (`ClearAsync` + `RebuildAsync(aggregate)`, a
  **multi-handler** contract, one rebuilder per read model) implemented per service, driven by
  one of **two** runners. `StateStoredReadModelRebuildRunner<TContext>` clears once and then streams
  every aggregate state out of the write database in batches; `EventSourcedReadModelRebuildRunner`
  does the same from Marten, collecting the distinct stream keys under the aggregate's
  `[AggregateName]` prefix and folding each stream through `LoadFromHistory` (ADR-0036 amendment
  2026-08-10). The **contract does not change with the path** — both share the internal
  `ReadModelRebuildWriter`, so a service writes one `IReadModelRebuilder` and nothing else. A runner
  is invoked by the context's migration worker behind a
  configuration switch, never automatically, and **throws** when no rebuilder is registered —
  a rebuild that projects nothing would report success over an empty read model. Three
  consequences to know: **every field must be a function of the current aggregate state** (write
  absolute values, never increments — a field that needs history belongs in an event-sourced
  context); the handover back to live traffic needs nothing new, because the rebuilder writes the
  aggregate's current `Version` and the existing watermark check discards what is already
  contained; and the rebuild does **not** run through the integration-event publisher, so it publishes no
  integration events and produces no cross-context replay. A **parity test is mandatory** where a
  context has both — one aggregate's events through the live projections, the same aggregate's
  final state through the rebuilders, both rows identical. That test is the whole reason two
  derivation paths are acceptable, and **both samples now carry one**.
- **In-context** projections use **domain** events directly; **integration** events
  (RabbitMQ) are the **only** cross-context signal — never read another context's
  database.
- **Broker topology** (ADR-0023 amendments): one topic exchange for the whole platform. Its
  **name comes from the host**, not from Building Blocks — VitalSync declares it once as
  `VitalSyncMessaging.IntegrationEventExchangeName` (`vitalsync.integration-events`) in
  `VitalSync.ServiceDefaults`, and every host passes it down. Never write the string as a
  literal in a host, and never move it back into Building Blocks: since the 2026-08-05
  amendment the string `vitalsync` does not occur anywhere under `BuildingBlocks/src`, which
  is what makes ADR-0018's independence promise literally true.
- **A context knows its own name** (ADR-0023 amendment 2026-08-05). The three transport
  coordinates are supplied together —
  `options.UseWolverineMessaging(rabbitMqUri, exchangeName, contextName)`. `contextName` is
  **mandatory** and a single kebab-case word; a dot in it is rejected, because that is almost
  always the exchange name in the wrong argument. Three rules hang off it:
  1. **Publish guard** — the prefix of `[IntegrationEventTopic]` must equal the context name.
     Publishing `fitness.…` from Nutrition throws instead of quietly impersonating another
     context.
  2. **Self-consumption is suppressed** — every published event carries the header
     `buildingblocks.source-context`, and `OwnContextIntegrationEventFilter` discards an
     incoming integration event whose source is the consuming context itself. This is
     provably lossless only because of rule 3.
  3. **Handler ⇒ pattern, checked at start-up** — for every integration event handled in the
     subscription's consumer assembly, at least one bound topic pattern must match its topic,
     and its topic must **not** be the consumer's own context. Both are hard start-up errors.
     The opposite direction (a pattern with no matching contract) stays unchecked on purpose:
     binding to an upstream context that does not exist yet is legitimate and locally
     undecidable.
- The publishing rule matches
  the `IIntegrationEvent` marker (`PublishMessagesToRabbitMqExchange<IIntegrationEvent>`)
  — **never** all messages, so
  `DomainEventEnvelope` cannot leak onto the broker. Every integration event **must**
  carry `[IntegrationEventTopic("<context>.<event>")]` in kebab-case
  (`nutrition.recipe-created`) — a Building Blocks attribute in
  `BuildingBlocks.Application`, so contract assemblies never reference Wolverine: the
  routing key is part of the published contract, not derived from the CLR namespace.
  The attribute rejects anything but two kebab-case segments at construction, and
  publishing an event without it **throws** instead of silently using a CLR-derived key.
  Consumers subscribe via `options.SubscribeToIntegrationEvents(queue, consumerAssembly,
  patterns)` — Building Blocks wires **both halves**, so the subscribing host adds
  nothing of its own. Pass the service's **Infrastructure** assembly,
  never its Application assembly: Wolverine discovers handlers by naming convention and
  would mistake `CreateRecipeHandler` for a message handler. Beware: Wolverine
  **silently discards** a message with no route, and a message whose consumer was never
  discovered is marked handled and dropped without a retry or a dead letter — both
  failures are invisible. A consumer that keeps throwing is retried three times and the message
  then goes to Wolverine's `wolverine-dead-letter-queue` **on the broker** (`DeadLetterTests`).
- **Where a dead letter lands depends on the endpoint, and both places exist.** An
  **integration event** arrives over a RabbitMQ listener, so giving up on it moves it to the
  broker queue above. A **projection envelope** travels on a *local* queue, which has no broker
  endpoint at all, so giving up on it writes a row into the `wolverine_dead_letters` table in the
  write database. The write-database table is therefore **not** empty by design — that was true
  only while every dead letter came from a broker listener. Since ADR-0022's amendment split the
  projection onto its own queue, that table is the only place a lost projection is recorded, which
  is why `DeadLetterHealthCheck` watches it (`DeadLetterVisibilityTests`).
- **A dead-lettered projection is visible as a `Degraded` health check**, never as an unhealthy
  one. `AddBuildingBlocks` registers `building-blocks-dead-letters` (tag `dead-letters`) whenever a
  persistence strategy was selected; it counts `wolverine_dead_letters` capped at 1000 and reports
  `Degraded` with the count. Degraded maps to HTTP 200, so the check is visible in `/health` and in
  the Aspire dashboard **without** kicking the service out of readiness — which is the point: the
  host still serves every request correctly, it is the read model that is missing a change. Do not
  "upgrade" it to `Unhealthy`: that returns 503, and Aspire would restart or drain a host whose
  only problem is a stale read model, turning a wrong number into an outage. A missing table also
  reports `Degraded`, because a check that cannot see failures must not report health.
- **Retries are graded by failure class** (ADR-0023 amendment 2026-08-06). Three rules, first
  match wins. **Hopeless** (`JsonException`, `DomainValidationException`,
  `BusinessRuleViolationException`) is dead-lettered on the **first** attempt — it never
  recovers, and retrying it writes four error logs where one is the truth, multiplying the
  metric every alert threshold is calibrated against. **Transient** (`NpgsqlException` with
  `IsTransient`, `TimeoutException`) retries over 1 s / 5 s / 15 s / 30 s and deliberately
  does **not** dead-letter — a failover outlasts any cooldown ladder, so the message stays on
  the queue for redelivery, which is safe only because of the 7-day idempotency window.
  **Unknown** keeps the old 100 ms / 500 ms / 2 s and then dead-letters. Match the
  `IsTransient` **predicate**, never the bare `NpgsqlException` type: a unique violation
  (`23505`) is not transient and must fall through to the unknown class, because turning it
  into a `Failure.Conflict` is a separate concern.
- **Integration-event delivery is durable** (ADR-0023 amendment 2026-08-04): the publish
  rule adds `UseDurableOutbox()`, so the sending endpoint is `EndpointMode.Durable`. That
  one setting decides two things at once — the AMQP persistence flag (`delivery_mode: 2`)
  **and** whether an outgoing envelope is written to `wolverine_outgoing_envelopes` — so
  neither a broker restart nor a process crash between commit and acknowledgement loses an
  event. The exchange and the subscriber queue are declared `IsDurable`, and
  `UseQuorumQueues()` is configured **on the transport**, not per queue, so it also covers
  the `wolverine-dead-letter-queue`. Because a durable sending endpoint needs a message
  store, `UseWolverineMessaging` **without** `UseEfCorePersistence` or
  `UseMartenEventSourcing` throws in `AddBuildingBlocks`; the check runs after the whole
  options lambda, so the call order does not matter. Pinned by
  `IntegrationEventDurabilityTests` (real broker) and `WolverineExtensionTests` (no Docker).
  Consequence to know: a queue's type is fixed at declaration, so a broker still holding a
  classic queue of the same name makes `AutoProvision` fail and the queue must be deleted —
  and since ADR-0037 only the provisioning host calls `AutoProvision` at all.
- **Publisher confirmations are on, and that closes the last metre** (2026-08-07).
  A durable outbox deletes its row once Wolverine considers the envelope sent, and without
  confirmations "sent" means *in the socket*, not *in the broker* — a broker that discards
  the message afterwards tells nobody. RabbitMQ.Client 7 moved both switches onto
  `CreateChannelOptions` and defaults them to `false`; Wolverine passes that default through
  unchanged. `ApplyBuildingBlocksMessagingDefaults` therefore calls `ConfigureChannelCreation`
  and enables **both** `PublisherConfirmationsEnabled` and
  `PublisherConfirmationTrackingEnabled` — enabling only the first is worse than enabling
  neither, because the broker then answers without a correlatable sequence number. With
  tracking on, `BasicPublishAsync` raises a `PublishException` on a `nack` or `basic.return`,
  so the failure surfaces where the retry policies already are and the outbox row survives.
  The price is a broker round trip per message (measured: ~1 150 msg/s versus ~62 500
  without, sequential single sends). Since the domain-event queues are partitioned per
  aggregate rather than globally serialised, this is a real per-message cost and no longer
  hidden behind a harder cap. The pinning test asserts
  first that a fresh `WolverineRabbitMqChannelOptions` has both flags **off**; without that
  anchor it would quietly become worthless the day Wolverine changes its own default.
- **Consumer idempotency has two halves, and only one is built here** (ADR-0023 amendment
  2026-08-06). Wolverine's **durable inbox already deduplicates**: the subscriber queue
  listens with `UseDurableInbox()`, every incoming envelope lands in
  `wolverine_incoming_envelopes` under a primary key on the envelope id, a second arrival
  raises PostgreSQL `23505` and the message is acknowledged **without** running a handler.
  The id crosses the wire as the AMQP `MessageId`, so a nack, a requeue, a crash before the
  ack, a broker reconnect and the sender's outbox retry are covered for free — do not build
  anything for those. What was broken is that Wolverine **deletes** handled rows after
  `DurabilitySettings.KeepAfterMessageHandling`, default five minutes, so the guarantee
  expired silently; `ApplyBuildingBlocksIdempotencyWindow` sets it to **7 days** whenever a
  persistence strategy was selected, and a test pins that the value is not the framework
  default. Uncovered is a republication under a **new** envelope id, and that case is now
  **closed rather than deferred**: ADR-0036 decided the read-model rebuild against a replay,
  and the rebuild does not run through `DomainEventPublisher`, so nothing in this system
  republishes an event under a fresh transport identity. The dedup table once planned for this
  case is **not built**. `IIntegrationEvent.EventId` (ADR-0029) still has to stay
  stable per event — it costs nothing and is the groundwork should a context ever switch to
  event sourcing and replay a stream onto the broker. Until then **shared
  identity is the sanctioned route** — a consumer deriving its own aggregate adopts the
  foreign id, the way `MirrorWidgetHandler` does. Never write a consumer that assumes
  exactly-once.
- **A mapper without a transport fails the host at start** (ADR-0023 amendment 2026-08-06).
  `IntegrationEventMapperCheck` throws, naming the mappers, when integration-event mappers
  are registered and every event they produce would reach `NullIntegrationEventSink` — a
  warning log while the commit reports success, indistinguishable downstream from an
  upstream context that has not published yet. The check asks about the **effect**, not the
  selection: it fires when the resolved `IIntegrationEventSinkFactory` is still the null
  one, **not** when `UseWolverineMessaging` was skipped, so a host supplying its own sink
  factory passes. Copy that shape for new checks — it is the same reason
  `UnitOfWorkPresenceCheck` probes `IUnitOfWork` instead of the persistence choice. There
  is deliberately **no `UseNoMessaging()`**: unlike `UseNoPersistence()`, which states an
  intent you cannot read off the code, "this host publishes nothing" is simply the absence
  of a mapper — so the remedy is to delete the dead mapper, and adding the opt-out is
  refused. The mirror case, a **domain event with no projection handler**, stays unchecked
  on purpose: several handlers and no handler are both legitimate.
- **The mapper contract is typed** (ADR-0023 amendment 2026-08-11).
  `IIntegrationEventMapper<in TDomainEvent>` is the twin of `IProjectionHandler<in TDomainEvent>`:
  registered through the same `MultiHandlerInterfaceDefinitions` path and resolved by
  `MapperRunner`, the twin of `ProjectionRunner` with the same cached invoker. The untyped
  predecessor ran every mapper for every domain event and filtered with a `switch` whose
  `_ => []` arm swallowed a missing case silently — which events leave the context is now
  readable from the type signature. Consequence for `IntegrationEventMapperCheck`: an open
  generic cannot be resolved, so the check closes `IIntegrationEventMapper<>` over every type
  in the `DomainEventTypeRegistry` and probes the container for each — a mapper is therefore
  only visible to it once its domain event's assembly was named by `AddDomainEventsFrom`,
  which is no restriction, since an unregistered domain event can neither be persisted nor
  published.
- **Snapshotting is deferred** but additive: a Marten snapshot is a separate document
  and the event schema is unchanged, so snapshots can be added per context later with
  **no event migration**.
- **A service host registers through the host-builder overload** `builder.AddBuildingBlocks(options => …, configureWolverine?)` (ADR-0027 amendment 2026-08-03) and calls **no `UseWolverine` at all** — Building Blocks issues it, and applies the EF Core outbox from the write connection string the host already named in `UseEfCorePersistence`. **The write database is named exactly once**; the earlier requirement to repeat it in the host's own `UseWolverine(...)` is gone, and with it the silent failure of outbox and aggregates landing in different databases. Wolverine permits only one `UseWolverine`, so host-specific transport settings go in the optional `configureWolverine` callback. This is the **only** way to get the EF Core outbox — the former public `UseBuildingBlocksEfCorePersistence(cs)` is deleted, so no host can point the message store at a second database. The `IServiceCollection` overload still serves handlers, Marten, and messaging for hosts that wire Wolverine themselves; a **state-stored** host must use the builder overload.
- **`Microsoft.EntityFrameworkCore.Design` belongs to the MigrationService, not to Infrastructure.**
  Infrastructure is referenced by the Api, the MigrationService and the tests, so a design-time
  package placed there travels into all of them; the worker is a leaf host. Declare it with
  `PrivateAssets="all"` and **no** `IncludeAssets` — the widely copied
  `IncludeAssets="runtime;build;native;contentfiles;analyzers"` drops `compile`, and the
  `IDesignTimeDbContextFactory` implementations stop compiling. (The older claim that
  `PrivateAssets` severs the edge to `EntityFrameworkCore.Relational` is wrong: Relational
  arrives through `Npgsql.EntityFrameworkCore.PostgreSQL` out of `BuildingBlocks.Infrastructure`.)
  Migrations stay in Infrastructure, so scaffolding names both projects — `--project` on
  Infrastructure, `--startup-project` on the MigrationService. The factories live in the worker
  and stay `internal` (EF Core finds them by reflection; `public` trips `CA1515`) and they are
  **required**, because `dotnet ef` otherwise builds the worker's host, which reads Aspire
  connection strings that do not exist at design time. Each sample's `DesignTimePackageTests`
  fails once the package reappears in Infrastructure or loses `PrivateAssets`.
- PostgreSQL is provisioned as a first-party **.NET Aspire** resource.

ADRs are immutable once accepted; to change a decision, add a superseding ADR.
Index: `docs/architecture/decisions/README.md`.

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
  add them to `VitalSync.slnx`. See `docs/architecture/testing-strategy.md`.
- Strategy covers unit, integration, domain, application-layer, persistence, and
  component-communication tests. See `docs/architecture/testing-strategy.md`.
- **CI is `.github/workflows/build.yml`** (push to `main`, PRs, manual): Release build, the whole
  suite with `VITALSYNC_REQUIRE_CONTAINERS=1` so container tests **fail instead of skipping**, then
  the samples AppHost is started and the smoke tests run against it with `SAMPLE_*_API_URL` and
  `VITALSYNC_REQUIRE_SMOKE=1` set (`SmokeRequirement` — a missing URL then fails instead of skipping).
  Keep both flags when touching the workflow — a green run that skipped the container and
  smoke tests is exactly the blind spot the walking skeleton exposed. The SDK is pinned by
  `global.json`; no Aspire workload is installed (the AppHosts use `Aspire.AppHost.Sdk` as a package).
- **The `Diagnostics` step is not decoration.** Aspire routes resource and container logs to the
  dashboard, not to the AppHost's stdout, so `apphost.log` alone shows a clean start and then
  nothing — a stalled container is indistinguishable from a broken one. The `if: failure()` step
  therefore also dumps `docker ps --all`, `docker images` and `docker logs --tail 200` per
  container, and the wait loop prints a container status line every sixth attempt. Without this
  a failed smoke stage can only be answered by re-running it.
- **Both test stages emit a TRX report (`--report-xunit-trx`), and that is load-bearing.** The
  console output names the *count* of failed tests but never the *name* — a red run says
  "Failed: 1, Passed: 244" and nothing else. The name used to live only in the 668 KB TestResults
  log inside the artifact, so diagnosing a red run cost a download, and once the artifact expired
  the information was gone for good. Each stage therefore has an `if: failure()` step that greps
  the TRX for `outcome="Failed"` and prints those entries with message and stack trace. Keep the
  flag and the step together: `--report-xunit-trx` without the step writes a file nobody reads,
  and the step without the flag finds nothing.

## When contributing

1. Put reusable, VitalSync-agnostic concepts in `BuildingBlocks`; put domain logic in the matching `src/Services/<Domain>` project.
2. Respect the communication rules (Frontend → BFF → services; async only between services).
3. Follow the DDD/CQRS/ES ADR conventions above.
4. Keep layer boundaries clean; don't leak infrastructure into the domain.
5. Add or update tests (mirror the project structure).
6. Write **no comments** — not in `*.cs`, `*.csproj`, workflow YAML, or the code examples in `*.md` (ADR-0028); delete any comment you come across.
7. If a change affects architecture, add or update an ADR using the template in `docs/architecture/decisions/README.md`.
8. Match existing style; respect `.editorconfig`, `Directory.Build.props` and `Directory.Packages.props`.
9. **Always work on `main` branch** — never work on separate branches, and never ask which branch to use; `main` is always the target.
10. Always update the instruction files in `.github/*.md` and `.claude/*.md` if you discover a gap or ambiguity in the guidance.
11. If you are unsure about a decision, **always ask a human** — Copilot is not the arbiter of architecture or domain rules.
12. Always use short and clear commit messages.
13. If you write code, always add or update unit tests / integration tests / architecture tests, and make sure they pass before committing.
14. Always check all `*.md` files in the repository and update them if needed.
15. **NEVER commit yourself.**

## Key documentation

- Architecture overview — `docs/architecture/overview.md`
- Communication — `docs/architecture/communication.md`
- Building Blocks — `docs/architecture/building-blocks.md`
- Domain model — `docs/architecture/domain-model.md`
- CQRS & Event Sourcing — `docs/architecture/cqrs-and-event-sourcing.md`
- Testing strategy — `docs/architecture/testing-strategy.md`
- ADRs — `docs/architecture/decisions/README.md`
- Glossary — `docs/glossary.md`
