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
  (`tests/VitalSync.ServiceDefaults.Tests`). `AddServiceDefaults()` also registers the
  OpenTelemetry sources `Npgsql`, `Wolverine`, and `Marten` (plus the `Npgsql` meter), so
  database and transport spans exist without any per-host wiring — do not re-add them.

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
  meant "rule passed". All four overloads guard with `ArgumentNullException.ThrowIfNull`; the
  `params` ones guard the array too. Short-circuiting is unchanged.
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
  `IntegrationEvents/` (root empty). Domain and integration events are **deliberately not**
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
  `IIntegrationEventMapper`) and enforces **exactly one** handler per command/query —
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
  are **translated to `Result.Failed`** by an Application pipeline behavior; unexpected
  errors bubble to a thin global handler (ADR-0017). `FailureCategory` is one of
  `Validation`, `BusinessRule`, `NotFound`, `Conflict` — transport status mapping is
  owned by the BFF/service host, never by `Application`.
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
  (ADR-0017 amendment 2026-08-05).
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
  `EntityKeyModelBuilderExtensions`. Seven more are public **only** because Wolverine
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
  `DependencyInjection/Validation/`.
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
  `StartAsync`, `AfterHostedServicesStarted` checks in its `StartedAsync`. Only the
  **phase** matters — the checks are pure readers, so their relative order decides only
  which message a broken host sees first, and the .NET host's three-pass start
  guarantees every `StartAsync` finishes before any `StartedAsync` begins. That is why
  `IntegrationEventSubscriptionCheck` can read Wolverine's handler graph without
  depending on a registration index. A check **registers unconditionally and guards
  itself** (early return when its capability was not selected) rather than being
  registered conditionally, and it probes the **built container**
  (`IServiceProviderIsService`), not the `IServiceCollection`.

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
- **Read models are owned by each service, not a Building Block** — the service owns
  its read-model schema, projection handlers, and queries; Infrastructure ships only
  the plumbing (Publisher, outbox, dispatch loop, projection runner, transport). Read
  models are **derived and rebuildable** by replaying events / re-running projections.
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
  then goes to Wolverine's `wolverine-dead-letter-queue` **on the broker** — not to the
  `wolverine_dead_letters` table in the write database, which stays empty (`DeadLetterTests`).
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
  classic queue of the same name makes `AutoProvision` fail and the queue must be deleted.
- **Snapshotting is deferred** but additive: a Marten snapshot is a separate document
  and the event schema is unchanged, so snapshots can be added per context later with
  **no event migration**.
- **A service host registers through the host-builder overload** `builder.AddBuildingBlocks(options => …, configureWolverine?)` (ADR-0027 amendment 2026-08-03) and calls **no `UseWolverine` at all** — Building Blocks issues it, and applies the EF Core outbox from the write connection string the host already named in `UseEfCorePersistence`. **The write database is named exactly once**; the earlier requirement to repeat it in the host's own `UseWolverine(...)` is gone, and with it the silent failure of outbox and aggregates landing in different databases. Wolverine permits only one `UseWolverine`, so host-specific transport settings go in the optional `configureWolverine` callback. This is the **only** way to get the EF Core outbox — the former public `UseBuildingBlocksEfCorePersistence(cs)` is deleted, so no host can point the message store at a second database. The `IServiceCollection` overload still serves handlers, Marten, and messaging for hosts that wire Wolverine themselves; a **state-stored** host must use the builder overload.
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

## When contributing

1. Put reusable, VitalSync-agnostic concepts in `BuildingBlocks`; put domain logic in the matching `src/Services/<Domain>` project.
2. Respect the communication rules (Frontend → BFF → services; async only between services).
3. Follow the DDD/CQRS/ES ADR conventions above.
4. Keep layer boundaries clean; don't leak infrastructure into the domain.
5. Add or update tests (mirror the project structure).
6. Write **no comments** — not in `*.cs`, `*.csproj`, workflow YAML, or the code examples in `*.md` (ADR-0028); delete any comment you come across.
7. If a change affects architecture, add or update an ADR using the template in `docs/architecture/decisions/README.md`.
8. Match existing style; respect `.editorconfig` and `Directory.Build.props`.
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
