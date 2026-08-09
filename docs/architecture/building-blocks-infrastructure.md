# BuildingBlocks.Infrastructure

`BuildingBlocks.Infrastructure` is the single outer layer of the Building Blocks
platform. It holds **all** reusable, framework-bound, third-party-backed
implementations that are still **independent of any VitalSync domain logic**
([ADR-0018](./decisions/0018-three-building-block-packages.md)). It depends on
`BuildingBlocks.Domain` and `BuildingBlocks.Application` and is the **only**
Building Block allowed to reference third-party packages.

> **Status: implemented.** This document is the authoritative design for the
> package. Where the implementation and this document diverge, treat the
> divergence as a bug in one of them and reconcile via a PR (updating this
> document if the design itself changed).

> Related decisions:
> [ADR-0015](./decisions/0015-hand-rolled-cqrs-mediator.md) (hand-rolled mediator),
> [ADR-0017](./decisions/0017-application-error-handling-and-result.md) (error handling),
> [ADR-0018](./decisions/0018-three-building-block-packages.md) (three packages),
> [ADR-0019](./decisions/0019-event-store-technology-marten.md) (Marten event store),
> [ADR-0020](./decisions/0020-postgresql-for-state-stored-contexts.md) (PostgreSQL),
> [ADR-0021](./decisions/0021-write-read-database-pair-per-context.md) (write/read DB pair),
> [ADR-0022](./decisions/0022-event-driven-read-models.md) (outbox-backed publisher),
> [ADR-0023](./decisions/0023-wolverine-messaging-transport.md) (Wolverine transport),
> [ADR-0024](./decisions/0024-contract-placement-innermost-consumer.md) (contract placement).

## Design rules

- **VitalSync-agnostic.** No references to VitalSync bounded contexts, aggregates,
  or concepts. Everything here must be reusable in any future project.
- **All third parties live here.** EF Core, Marten, Wolverine, Npgsql, and DI
  abstractions are referenced **only** in this package (and service hosts).
  `Domain` and `Application` stay dependency-free (ADR-0018).
- **Implements `Application` contracts; defines none of its own use-case contracts.**
  Where a new abstraction is needed that services must see, the contract goes to
  the innermost layer that consumes it (ADR-0024).
- **Nothing depends on Infrastructure** except service hosts / composition roots.
  Services reference it for DI registration and receive everything else through
  the `Domain` / `Application` abstractions.
- **Async-only.** Same rule as `Application`: every operation returns `Task<...>`
  and accepts a `CancellationToken`.
- **`internal` unless something outside genuinely needs the type.** A host consumes
  Infrastructure through DI, so an implementation registered in the container has no
  reason to be visible. The public surface is exactly six types plus the handful that
  Wolverine's runtime code generation forces open (see below), and
  `PublicSurfaceTests` fails the build if it grows.

## Public surface

Everything else is `internal`, with `InternalsVisibleTo` for the test assembly.

| Type                              | Why it is public                                                  |
| --------------------------------- | ----------------------------------------------------------------- |
| `ServiceCollectionExtensions`     | `AddBuildingBlocks` — the entry point                             |
| `HostApplicationBuilderExtensions`| `AddBuildingBlocks` on the host builder (ADR-0027)                |
| `BuildingBlocksOptions`           | the configuration surface passed to it                            |
| `EntityKeyModelBuilderExtensions` | `ApplyEntityKeyConversions`, called from a host's `DbContext`     |
| `PersistedSchema`                 | the event-schema snapshot, called from a service's tests (ADR-0035) |
| `ReadModelRebuildRunner<TContext>`| the read-model rebuild driver, constructed by a migration worker (ADR-0036) |

Seven further types are public **only** because Wolverine generates C# at runtime and
the generated code names them: `DomainEventEnvelope`, `DomainEventEnvelopeHandler`,
`DomainEventEnvelopeSerializer`, `DomainEventTypeRegistry`, `IIntegrationEventSinkFactory`,
`IntegrationEventSourceContext`, `OwnContextIntegrationEventFilter`. Generated code lives
in another assembly, so an `internal` type there fails — at run time for a service-located
dependency, at compile time for a handler's constructor parameter. This is a constraint of
the transport, not a design intent; `PublicSurfaceTests` lists them separately so the
distinction survives.

## Capabilities (internal organization)

`Infrastructure` is deliberately one package (ADR-0018), organized internally by
capability. The folder is the namespace.

| Folder                             | Capability                                                          |
| ---------------------------------- | ------------------------------------------------------------------- |
| `Dispatching/`                     | DI-based `ISender` implementation + pipeline behaviors              |
| `Persistence/`                     | shared: aggregate reconstitution, tracking base, envelope factory, typed-key conversion for EF Core and JSON |
| `Persistence/StateStored/`         | EF Core write path — repository, unit of work, tracker, state graph |
| `ReadModels/`                      | the read-model rebuild runner — host-invoked, therefore public and outside `Persistence/` |
| `Persistence/EventSourced/`        | Marten write path — repository, unit of work, tracker               |
| `Messaging/DomainEvents/`          | in-context events: publisher, projection runner, type registry, serializer, handler |
| `Messaging/IntegrationEvents/`     | the cross-context contract: topics, sink, source context, filter    |
| `Time/`                            | `IClock` implementation on top of `TimeProvider`                    |
| `Telemetry/`                       | the `ActivitySource` and the tag names the three instrumented paths use |
| `DependencyInjection/`             | entry points, options, and the composition root                     |
| `DependencyInjection/Registration/`| what a host selected, one collaborator per capability               |
| `DependencyInjection/Wiring/`      | how Wolverine itself is configured                                  |
| `DependencyInjection/Validation/`  | the start-up checks (ADR-0027)                                      |
| `Schema/`                          | the persisted-event snapshot a service's tests compare against (ADR-0035) |

Two cuts here carry meaning rather than tidiness:

- **`StateStored` and `EventSourced` are mutually exclusive.** A bounded context picks
  one (ADR-0019/0020), and the selection throws when a host selects both. The
  folders make that visible; the shared parent holds only what both need. Note that
  `ApplyEntityKeyConversions` stays in the shared parent on purpose: an event-sourced
  context still uses EF Core for its **read** models.
- **Configuring Wolverine is not messaging.** `WolverineWiringSettings`,
  `MessagingSettings`, `IntegrationEventSubscription`, `WolverineOptionsExtensions`, and
  `BuildingBlocksWolverineExtension` describe what the host asked for and translate it
  into Wolverine's options; they run once at composition time and never on a message.
  They live under `DependencyInjection/Wiring/`, not under `Messaging/`.

### The persistence selection is one value, not a set of flags

What a host selected is a single `PersistenceChoice` — a closed hierarchy of exactly
`None`, `Marten`, and `EfCore(connectionString)`, whose subtypes are private so no
fourth case can appear elsewhere. Everything Wolverine needs to know is derived from
it rather than stored beside it: `IsSelected` answers both "route domain events" and
"a message store exists" (they are the same fact), and `EfCoreWriteConnectionString`
is non-null for exactly one case. That is why the outbox cannot end up pointed at a
different database than the aggregates — there is no second place to write the
connection string to.

`WolverineWiringSettings` therefore has no public setters. Selecting is a method, and
the two guards that need nothing but the selection itself live there: choosing two
different strategies throws (naming both calls), and choosing the **same** strategy
twice with **different** arguments throws too, because a bounded context has exactly
one write database (ADR-0021). Repeating an identical call stays legal — the choice is
a record, so it compares by value. The cross-cutting checks that need the whole picture
(subscription without messaging, messaging without persistence) stay in the composition
root's `Validate` phase, since they are order-independent and only decidable once the
options lambda has run.

### Typed keys are converted, never discovered

EF Core's property discovery does **not** find a property whose type is an `IEntityKey<T>` — it is
"not a supported primitive type". `ApplyEntityKeyConversions` used to compensate by scanning the
CLR type and calling `AddProperty`, which meant a helper that wrote to the model and could
therefore contradict it: an `Ignore()`d key came back as a column, and a computed get-only key
broke model creation with "No backing field could be found".

Since ADR-0033 the helper only walks the properties the model already has and attaches an
`EntityKeyValueConverter<TKey, TValue>` to each one of key type, skipping any that already carries
a converter. It adds nothing, so it can override nothing. The price is that a `DbContext` maps
every typed key explicitly — which every context here already does, because it also wants column
names, `IsRequired` and `IsConcurrencyToken`. Forget one and EF Core fails when the model is
built, naming the property, its type and both remedies; `EntityKeyConversionTests` pins that
failure alongside the owned-type case.

Complex types are out of scope: `ComplexProperty` appears nowhere in the repository, write-side
children are owned types by ADR-0031, and read models are flat. See WS-15.

### A typed key serializes as its bare value

`IsEmpty` is a domain predicate derived from `Value`, but to a JSON serializer it is an ordinary
public property. A typed key therefore used to reach three append-only or contractual stores as
`{"Value":"8f3a…","IsEmpty":false}`: Marten's `mt_events.data`, the outbox payload, and the
integration-event body on RabbitMQ. None of the three shapes was chosen; all three came from a
default, and events are immutable.

Since ADR-0034 a key writes as its **bare underlying value** (`"GadgetId": "8f3a…"`).
`EntityKeyJsonConverterFactory` builds an `EntityKeyJsonConverter<TKey, TValue>` for any
`IEntityKey<TValue>` and reads it back through the same single-argument constructor the EF Core
value converter already requires — both share `EntityKeyActivator<TKey, TValue>`.
`EntityKeyJsonOptions` is the single place that attaches the factory, and it is applied at all
three sites: the envelope serializer's options, Marten via `UseSystemTextJsonForSerialization`, and
Wolverine via `UseSystemTextJsonForSerialization` in `BuildingBlocksWolverineExtension`.

Marten runs on System.Text.Json as part of that decision. A `[JsonIgnore]` on `IEntityKey.IsEmpty`
would have been smaller, but it binds to one serializer and Marten's default is the other — the
attribute would have been silently ineffective exactly where the immutable data lives. The read
side deliberately does not accept the old object shape; there are no streams to be compatible with.

### A persisted field name is pinned by a snapshot

ADR-0030 removed derived names at the type level and left the field level open: without an explicit
`[JsonPropertyName]` the JSON name of a field is the CLR property name, so renaming `Titel` to
`Name` renames the field on the wire. Stored events keep the old name and deserialize to `default`
— no exception, no log entry, no failing test.

ADR-0035 answers this with visibility rather than tolerance. `PersistedSchema` renders every domain
event and integration event of a set of assemblies into a deterministic text file and compares it
against an approved baseline that lives with the service that owns the events:

```text
domain-event widget-created-v1
  Name : string
  WidgetId : guid
```

The rendering reads through **`JsonTypeInfo`**, so it pins what the serializer actually does
(including a `[JsonPropertyName]` or a future `PropertyNamingPolicy`) rather than what reflection
sees. A typed key renders as the value it serializes to, which keeps ADR-0034 visible in the
baseline. Everything is sorted by name, because reordering members is meaningless for JSON.

The decision rule travels in the failure message: a field that was only **added** stays readable, so
approve the new snapshot; a field that was renamed, removed or retyped does not, so leave the event
untouched and introduce a successor under a new `[EventName]`.

The state-stored path needs no snapshot — a relational schema can contradict, and the EF migration
history is that path's baseline — but the same "declared, never derived" rule now holds there by
force: `AggregateStateModelCheck` rejects at start-up any property of an `AggregateState` or of one
of its owned children without an explicit `HasColumnName`, or without `HasJsonPropertyName` for a
`ToJson()` child.

---

## 1. CQRS dispatcher (`ISender` implementation)

Implements the `ISender` contract from `BuildingBlocks.Application`
(ADR-0015):

- Resolves the single matching `ICommandHandler<...>` / `IQueryHandler<...>`
  from the DI container.
- Wraps the handler in the registered `IPipelineBehavior<,>` chain, handing each
  behavior a `RequestPipeline<TResponse>` rather than a bare continuation. The
  sender builds that object where the response type is still concrete (`Result` for
  a void command, `Result<TResult>` for a query or value-returning command) and
  passes the matching `Failed` factory into it, so a short-circuiting behavior can
  produce a typed failure without reflection or a generic constraint
  (ADR-0015 amendment 2026-08-05).
- **Behavior ordering is an explicit numeric order** — each behavior is registered
  with an `order`, and the sender wraps them by ascending order (lower orders wrap
  further out and execute earlier); no attribute- or convention-based ordering, and
  no reliance on registration call order. Hosts add their own behaviors at a chosen
  position via `BuildingBlocksOptions.AddPipelineBehavior(type, order)` — that is the
  **only** supported way; a behavior registered directly on the `IServiceCollection`
  has no order and fails `AddBuildingBlocks` (see §Composition root).
- No reflection-heavy scanning at dispatch time; the closed-generic dispatcher is
  cached **per request/result type pair** after first use (the result type is part
  of the cache key, so a request type exposing two result contracts still resolves
  the correct dispatcher — the startup validator rejects such types in scanned
  assemblies anyway, since a command/query has exactly one result type).

### Pipeline behaviors shipped here

| Behavior                    | Order | Responsibility |
| --------------------------- | ----- | -------------- |
| `LoggingBehavior`           | `0` (outermost) | Structured logging of request name, outcome (success/failure categories), and duration. Never logs payload contents by default. Being outermost, translated failures (expected domain errors, concurrency conflicts) are logged at `Warning`, while only genuinely unexpected exceptions are logged at `Error` (faulted) and rethrown. |
| `ExceptionToResultBehavior` | `100` | Translates `DomainValidationException` / `BusinessRuleViolationException` into `Result.Failed` (ADR-0017), before logging sees the outcome and before any transaction is opened. Unexpected exceptions pass through untouched. |
| `UnitOfWorkBehavior`        | `300` (innermost) | Commits the unit of work when a **command** completes successfully — including the atomic outbox write (see §2/§4). Queries and failed results are unaffected: nothing is committed, and query-only hosts need no `IUnitOfWork` registration. The dependency is **non-optional**: Building Blocks registers a `NullUnitOfWork` fallback so the pipeline always resolves, and `UnitOfWorkPresenceCheck` fails the host at start when that fallback would silently swallow real commands (see §Composition root). A host that deliberately commits nothing says so with `UseNoPersistence()`. Translates the store's optimistic-concurrency exceptions raised on commit (Marten's `ConcurrencyException`, EF Core's `DbUpdateConcurrencyException`) into a `Failure` with category `Conflict`, and does the same for a PostgreSQL unique-constraint violation (SQLSTATE `23505`) — expected on both persistence paths, so it is caught both wrapped in a `DbUpdateException` and bare, as Marten raises it. |

The canonical execution order is therefore:
`Logging → ExceptionToResult → UnitOfWork → handler`.

Logging sits outermost so that expected domain errors surface as `Warning` (with
their failure categories) rather than `Error`, keeping the error rate a meaningful
alerting/SLO signal. The gap between `ExceptionToResult` (`100`) and
`UnitOfWork` (`300`) leaves slot `200` free for a future input-validation behavior
that must run outside the transaction. By convention, a host-supplied behavior uses
a negative order to run before all built-ins, or an order above `300` to run after
them.

## 2. Unit of work

```csharp
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct);
}
```

- **One unit of work per command dispatch**, owned by `UnitOfWorkBehavior`.
- On commit, in a **single write-database transaction**:
    1. persist aggregate changes (EF Core `SaveChanges` or Marten stream append);
    2. collect the aggregates' uncommitted domain events;
    3. mint each event's `EventId` and `OccurredAt` onto its envelope (one
       `IClock.Now` value shared by all events of the commit — ADR-0029; the
       events themselves stay untouched);
    4. write those envelopes to the **transactional outbox** in the write database
       (ADR-0022, ADR-0023) — atomically with the state change;
    5. clear the aggregates' event collections.
- **Optimistic-concurrency conflicts surface at commit, not before.** Both
  stores only detect a version conflict when the changes are flushed
  (`SaveChanges` / `SaveChangesAsync`); repositories only hand aggregates to
  the tracker and therefore cannot observe these exceptions. The commit path —
  via the `UnitOfWorkBehavior` (§1) — translates them into a `Conflict`
  failure.
- **A unique-constraint violation is a business outcome, not a crash.** "This
  name already exists" is expected, so `UnitOfWorkBehavior` maps SQLSTATE
  `23505` to a `Conflict` failure carrying the violated constraint's name —
  otherwise the caller gets a 500 and the error metric counts a system fault
  for a correctly working system. Mapping a constraint name onto a *business*
  code (`ux_recipes_name` → `recipe.name_taken`) belongs to the service, not
  here.
- Two implementations, one per persistence style, sharing the same behavior:
  an EF Core–backed unit of work and a Marten-session–backed unit of work.
  Wolverine's native Marten/EF Core integration provides the shared
  transaction + outbox enlistment (ADR-0023).
- **Steps 2 and 3 have exactly one implementation, not one per persistence style.**
  Tracking is `AggregateTracker<TEntry>`, whose subclasses add only what their store
  needs to know about an entry; both entry types expose the same `ITrackedAggregate`
  view (aggregate, aggregate name, aggregate id, current version). Envelope minting is
  `DomainEventEnvelopeFactory.WrapUncommitted(entries)` — the single place that reads
  `IClock.Now` **once per commit** and counts each event's per-aggregate `Version`
  backwards from the aggregate's current version, so ADR-0029 (identity is minted at
  commit) and ADR-0030 (the version is the projection watermark) are enforced in one
  file rather than in two that must be kept in step. A unit of work therefore contains
  no envelope arithmetic at all.

## 3. Generic repositories

Both persistence styles implement the **same** `IRepository<,>` contract from
`Application` ([ADR-0026](./decisions/0026-single-repository-contract.md)):

```csharp
public interface IRepository<TAggregate, in TKey>
    where TAggregate : class, IAggregateRoot<TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken ct);
    Task AddAsync(TAggregate aggregate, CancellationToken ct);
}
```

- Works against the context's **write database** only (ADR-0021).
- No `Update`/`Save` method — retrieved aggregates are tracked; changes flow
  through the unit of work.
- No `Remove` method — removal is a soft-delete state change in the domain and
  therefore an ordinary update (ADR-0026).
- **No query methods beyond `GetByIdAsync`.** Queries never go through
  repositories: the read side reads its own read database directly
  (ADR-0021/0022).
- Both implementations obtain the empty hull they rehydrate into through the
  internal `AggregateFactory`, which resolves and caches each aggregate's
  **private parameterless constructor** — no `new()` constraint, no public
  constructor required, and no asymmetry between the two paths. The convention
  is validated at host startup: `AddBuildingBlocks` checks every aggregate in
  the `AddDomainEventsFrom` assemblies and fails registration with the
  aggregate's name if the constructor is missing
  ([ADR-0025](./decisions/0025-unified-state-fold-aggregate-model.md)
  reconstitution amendment 2026-08-04).
- Both `AddAsync` implementations **reject aggregates with an empty identity**
  (`Id.IsEmpty`) with an `InvalidOperationException`: an aggregate gains its
  identity through its first event, and an empty hull exists only for
  rehydration — without the guard it would silently persist a `Guid.Empty` row
  or open a `…/00000000-…` stream.

### EF Core repository (state-stored contexts, ADR-0020)

- Strongly typed identifiers (ADR-0005) are mapped via EF Core value converters
  provided here as reusable conventions.
- What EF Core maps is the aggregate's **state**, not the aggregate (ADR-0025
  state-mapping amendment), so the change tracker cannot answer "which aggregates
  took part in this command". `EfCoreAggregateTracker` — the mirror image of
  `MartenAggregateTracker` — answers it instead: the repository registers every
  aggregate it hands out, together with the `IStateOwner` view it already
  resolved and the state instance EF Core tracks.
- **Load:** `FindAsync(stateType, [id])` → empty hull via the private
  parameterless constructor → `IStateOwner.Restore(state)`, then track. Owned
  children arrive with their owner — EF Core loads owned dependents eagerly, so
  no `Include`, no `AutoInclude` and no recursive `LoadAsync` is involved
  ([ADR-0031](./decisions/0031-aggregate-child-collections-as-owned-types.md)).
- **Commit (unit of work):** hands each entry's current state to
  `AggregateStateGraph.Reconcile`, which copies the scalars onto the tracked
  entity via `CurrentValues.SetValues` — load-bearing, not defensive: states are
  immutable, so every applied event left the tracked instance stale and without
  the copy a rename would be silently lost — and then reconciles the **owned
  graph**: a child that still exists has its scalars copied onto the tracked child
  and is recursed into, a new child is added to the tracked collection, a
  vanished one is removed from it — matched by **key**, at any depth. EF Core
  turns that into `UPDATE` / `INSERT` / `DELETE` with stable row identity
  (ADR-0031). Merely assigning a replacement collection would work only one level
  deep; the grandchildren it carries collide with the ones EF Core already tracks
  under the same key. A `ToJson()` collection is the exception and stays on the
  assignment path, since it is a single column whose dependents carry a
  synthesized shadow key.
- **Child collections are owned types, and that is enforced.** A navigation from
  an `AggregateState` to an **independent** entity type is rejected at host
  startup by `AggregateStateModelCheck`, naming the state and the
  navigation — such a model would be loaded by nothing and saved by nothing. The
  same validator rejects an owned collection that is not mapped to JSON and does
  not declare a single, non-shadow key: without that key the commit cannot match
  a replaced child against the tracked one. It also rejects any property of a
  state or of an owned child whose stored name is left to convention: an explicit
  `HasColumnName`, or `HasJsonPropertyName` for a `ToJson()` child, keeps a CLR
  rename free of charge instead of turning it into a destructive migration
  (ADR-0035). A child collection whose runtime
  value is read-only, fixed-size or `null` is rejected with a
  `NotSupportedException` at any depth of the graph, because EF Core adds and
  removes dependents through the collection instance itself. Authoring rules for
  a state with children: the collection is a `{ get; init; }` property (a
  positional record parameter makes the state unconstructible for EF Core) and is
  built with `ToList()` — a collection expression assigned to
  `IReadOnlyCollection<T>` compiles to a read-only array, never to a `List<T>`.

### Marten event-sourced repository (ADR-0019)

A small cluster of classes, using Marten as a **raw stream store**:

- `MartenEventSourcedRepository<TAggregate, TKey>` — the repository itself;
- `MartenAggregateTracker` — a scoped tracker the repository registers every
  aggregate it hands out (loaded or added) with, mirroring EF Core's change
  tracker; each `TrackedAggregate` entry carries accessors for the stream key
  and the expected version, so the Marten unit of work can append the
  uncommitted domain events, enroll them in the outbox at commit time, and
  defer clearing the event collections until after a successful commit (§2);
- `EntityKeyFormatter` — derives the stream key from the aggregate type and its
  strongly typed identifier (`{AggregateType}-{Id}`), since Marten streams are
  keyed by string (`StreamIdentity.AsString`) while aggregates use strongly
  typed keys (ADR-0005).

- **Load:** `FetchStream(id)` → fold via
  `((IEventSourcedAggregateRoot<TKey>)aggregate).LoadFromHistory(history)`,
  then track the aggregate. Marten's `Apply`-on-aggregate convention is
  **never** used (preserves ADR-0010 / ADR-0025).
- **Add:** tracks the new aggregate; no session interaction happens in the
  repository at all.
- **Commit (unit of work):** appends each tracked aggregate's uncommitted
  events to its stream with expected-version optimistic concurrency asserted
  against the aggregate's `Version`. A wrong expected version surfaces Marten's
  `ConcurrencyException` when the session is saved; the commit path translates
  it into a `Failure` with category `Conflict`.
- **Snapshotting is deferred** (ADR-0019): when a context later needs it, the
  repository reads the latest snapshot document, then folds only the stream tail
  via the aggregate's `FromState` seed — a purely additive change.

## 4. Transactional outbox & Publisher (ADR-0022, ADR-0023)

- **Write:** every uncommitted domain event of a tracked aggregate is wrapped
  in a `DomainEventEnvelope` — the single concrete message type this package
  publishes, since Wolverine dispatches by exact concrete type and cannot
  route by the open `IDomainEvent` interface — and handed to Wolverine's
  outbox (`IDbContextOutbox<TContext>` for EF Core, `IMartenOutbox` for
  Marten) **inside the write transaction** (same unit of work as the state
  change).
- Wrapping and unwrapping are centralized in a `DomainEventEnvelopeSerializer`
  so the two unit-of-work implementations and the envelope handler share one
  serialization scheme. The envelope carries the event's **declared name** from
  its `[EventName]` — never `AssemblyQualifiedName`, and resolved through the
  closed `DomainEventTypeRegistry` rather than `Type.GetType` (ADR-0030) — plus
  its identity (`EventId`, `OccurredAt`, minted at commit, ADR-0029) and the
  aggregate metadata (`AggregateName`, `AggregateId`, `Version`). The handler
  passes all of it on as `DomainEventMetadata`. Each event in a commit gets its
  own version, counted back from the aggregate's final version, so a projection
  sees a strictly increasing per-aggregate sequence.
- The same registry feeds `options.Events.MapEventType` in the Marten wiring, so
  the event store writes the declared name into `mt_events.type` instead of
  deriving one from the CLR type name. Renaming an event class is therefore a
  refactoring, not data loss — in the outbox **and** in the event store.
- **Dispatch:** after commit, Wolverine delivers each envelope to the single
  `DomainEventEnvelopeHandler` this package registers, which unwraps it and
  calls the **Publisher**. Both persistence paths flush the outbox
  **immediately after a successful commit** (commit first, then flush): EF Core
  atomically via `SaveChangesAndFlushMessagesAsync`, Marten via the
  flush-on-commit session listener that `IMartenOutbox.Enroll` registers — the
  durability agent's polling is crash-recovery fallback only, not the normal
  delivery path. The Publisher dispatches the unwrapped event to:
    - **in-context projection handlers** (via the projection runner, §5), which
      update the context's **read database**;
    - the **integration-event path** (§6) for events selected for cross-service
      communication.
- Delivery is **at-least-once**: a failed dispatch is retried by Wolverine's
  own outbox, not by any custom drain loop.
- The envelope is routed to a durable, strictly sequential local queue, so a
  single aggregate's events cannot be reordered relative to one another by a
  crash-triggered redelivery (applied automatically by the registered
  Wolverine extension, see §7 / ADR-0027).

## 5. Projection runner (domain-event dispatching)

- Invokes the in-context projection handlers registered against a domain-event
  type. The **handler abstraction lives in `Application`** (ADR-0024); only the
  runner lives here.
- Enforces/enables the ADR-0022 handler rules:
    - **Idempotency** is the handler's responsibility (upsert by key), tracked
      via the envelope's stable `EventId` (minted once at commit, ADR-0029) as
      the last-processed marker — this works identically for
      event-sourced and state-stored aggregates, which do not all have a
      meaningful stream position.
    - **Per-aggregate ordering** is guaranteed by the messaging transport's
      sequential local queue (§4), not by the runner itself.
- Read models themselves are **not** part of this package — they are
  domain-shaped and belong to each service (ADR-0022). Infrastructure ships
  plumbing only.

## 6. Integration-event messaging (Wolverine + RabbitMQ, ADR-0023)

- **Wolverine** is the messaging library; **RabbitMQ** is the broker,
  provisioned as an Aspire resource. Only **integration events** cross the
  broker (ADR-0004/0023).
- Publishing: the Publisher translates selected domain events into integration
  events (translation maps live in each service, not here) and hands them to
  Wolverine, which delivers them reliably from the shared outbox.
- Consuming: incoming Wolverine handlers are **thin adapters** that call
  `ISender` (ADR-0023 scope note) — Wolverine is the transport, **never** the
  in-process mediator. The `Result` model, exception translation, and pipeline
  ordering remain authoritative.
- **Retries are graded by failure class** (ADR-0023 amendment 2026-08-06). One rule for
  every exception served two opposite failure classes badly, so
  `ApplyBuildingBlocksMessagingDefaults` registers three, and Wolverine takes the first
  that matches. **Hopeless** — `JsonException`, `DomainValidationException`,
  `BusinessRuleViolationException` — is dead-lettered immediately: it never recovers, and
  retrying it writes four error log entries where one is the truth, multiplying the metric
  every alert threshold is calibrated against. **Transient** — an `NpgsqlException` whose
  `IsTransient` is set, and `TimeoutException` — retries over 1 s / 5 s / 15 s / 30 s and
  deliberately does **not** end in `MoveToErrorQueue`: a failover outlasts any cooldown
  ladder, so the message stays on the queue for the broker to redeliver, which is safe
  precisely because of the 7-day idempotency window below. **Unknown** — anything else —
  keeps the original 100 ms / 500 ms / 2 s and then dead-letters. The transient rule
  matches the `IsTransient` **predicate**, not the type, so a unique violation (`23505`)
  falls through to the unknown class instead of being retried as if the database were
  ill.
- **Routing.** Connecting the transport moves nothing on its own — Wolverine
  routes only what a routing rule matches, and `PublishAsync` silently discards
  an unroutable message. `UseWolverineMessaging` therefore installs the rule
  `PublishMessagesToRabbitMqExchange<IIntegrationEvent>(exchangeName, …)` with the
  exchange name the host supplied.
  Matching the marker rather than all messages is load-bearing: `DomainEventEnvelope`
  does not implement `IIntegrationEvent` and so can never be routed onto the
  broker (ADR-0022/0023, pinned by `IntegrationEventRoutingTests`). Each
  integration event supplies its routing key via a mandatory
  `[IntegrationEventTopic("<context>.<event>")]` attribute from
  `BuildingBlocks.Application` — resolved by the routing rule's topic source, so
  publishing an event without it throws instead of silently using a CLR-derived
  key (ADR-0023 amendment 2026-08-03).
- **Context identity.** The host also names its own bounded context
  (ADR-0023 amendment 2026-08-05). The topic source compares the first segment of
  every routing key against that name and **throws** when they differ, so a service
  cannot publish under another context's identity. Every published event carries the
  header `buildingblocks.source-context`, and a consumer-side middleware discards an
  integration event whose source is the consuming context itself.
- **Durability.** The topology is declared durable end to end (ADR-0023 amendment
  2026-08-04). The exchange and the subscriber queue are declared `IsDurable`, and
  queues are **quorum** queues configured on the transport rather than per
  declaration, which also covers the queues Building Blocks never names itself —
  above all Wolverine's `wolverine-dead-letter-queue`. The publish rule adds
  `UseDurableOutbox()`, which puts the sending endpoint into `EndpointMode.Durable`;
  that one setting decides two things at once, because Wolverine's RabbitMQ sender
  derives the AMQP persistence flag from the endpoint mode **and** only a durable
  endpoint writes an outgoing envelope to `wolverine_outgoing_envelopes`. Buffered —
  the framework default this package used to inherit — meant a broker restart lost
  the message and so did a process crash between commit and acknowledgement. Pinned
  by `IntegrationEventDurabilityTests` against a real broker and by
  `WolverineExtensionTests` without Docker.
- **Publisher confirmations close the last metre.** A durable outbox deletes its row
  once Wolverine considers the envelope sent, and without confirmations "sent" means
  *in the socket*, not *in the broker* — a broker that discards the message
  afterwards tells nobody. RabbitMQ.Client 7 moved both switches onto
  `CreateChannelOptions` and defaults them to `false`; Wolverine passes that default
  through unchanged. `ApplyBuildingBlocksMessagingDefaults` therefore calls
  `ConfigureChannelCreation` and enables **both** `PublisherConfirmationsEnabled` and
  `PublisherConfirmationTrackingEnabled`. Enabling only the first is worse than
  enabling neither: the broker answers, but without a correlatable sequence number.
  With tracking on, `BasicPublishAsync` raises a `PublishException` on a `nack` or a
  `basic.return`, so the failure surfaces where the retry policies already are and
  the outbox row survives. The price is a broker round trip per message — measured at
  roughly 1 150 msg/s against ~62 500 without, sequential single sends. That is not
  the bottleneck: domain events already pass through a `.Sequential()` queue
  (TODO-20), which caps throughput harder. Pinned by
  `Configure_WithBrokerUri_EnablesPublisherConfirmationsAndTheirTracking`, which
  first asserts that a fresh `WolverineRabbitMqChannelOptions` has both flags off —
  without that anchor the test would quietly become worthless the day Wolverine
  changes its own default.
- **Consumer side included.** Queue declaration, binding, listening, and consumer
  discovery are wired by `SubscribeToIntegrationEvents` (ADR-0023 amendment
  2026-08-01); the subscribing host adds nothing of its own.
- **The inbox idempotency window is a decision.** The subscriber queue listens with
  `UseDurableInbox()`, so every incoming envelope is stored in
  `wolverine_incoming_envelopes` under a primary key on the envelope id; a second
  arrival of the same id violates that key and Wolverine acknowledges the message
  without running a handler. Since the id crosses the wire as the AMQP `MessageId`,
  that already covers a nack, a requeue, a crash before the ack, a broker reconnect
  and the sender's own outbox retry. Wolverine then **deletes** those rows after
  `DurabilitySettings.KeepAfterMessageHandling`, whose default is five minutes, so
  the guarantee silently expired. `ApplyBuildingBlocksIdempotencyWindow` widens it to
  **7 days** whenever a persistence strategy was selected — without a message store
  there are no inbox rows to keep. Seven days covers a weekend plus the time an
  operator needs to replay a message out of the dead-letter queue. What this does
  **not** cover is a republication under a *new* envelope id — a case that does not
  arise here: ADR-0036 derives a state-stored read model from the current aggregate
  state instead of replaying events, and that rebuild never reaches
  `DomainEventPublisher`. A dedup table keyed by `IIntegrationEvent.EventId` is
  therefore **not built**; the id stays stable per event so the option survives.

### Runtime code generation

The package references **`WolverineFx.RuntimeCompilation`**. Wolverine 6 removed
the Roslyn compiler from its core package, while its default `TypeLoadMode`
generates and compiles handler code at runtime — without the package the first
handler codegen fails. The package self-activates when referenced, so hosts keep
configuring nothing (ADR-0027) and the dependency reaches them transitively.

The cost is Roslyn in the deployment. Pre-generating all code
(`TypeLoadMode.Static` plus a `codegen write` build step per host) removes it and
enables AOT; that is an additive optimisation deliberately deferred to its own
ADR, not an oversight.

## 7. Dependency-injection wiring

A small set of `IServiceCollection` extensions is the package's public surface
for hosts, e.g.:

```csharp
services.AddBuildingBlocks(options =>
{
    options.AddHandlersFrom(typeof(SomeHandler).Assembly);
    options.UseEfCorePersistence<NutritionWriteDbContext>(writeConnectionString);
    options.UseMartenEventSourcing(writeConnectionString);
    options.UseWolverineMessaging(rabbitMqUri, exchangeName, "nutrition");
});
```

- Registers `ISender`, the behaviors **in the canonical order** (§1), the unit
  of work, repositories, the Publisher/outbox, the projection runner, the
  Wolverine transport, and the clock (§8).
- `UseEfCorePersistence<TContext>(connectionString, configureContext?)`
  **registers the write-database context itself** via Wolverine's
  `AddDbContextWithWolverineIntegration` on the Npgsql provider (ADR-0027) —
  the host never registers the context and therefore cannot break the
  single-transaction outbox guarantee with a plain `AddDbContext`. Aspire
  hosts *enrich* the registration afterwards (e.g. `EnrichNpgsqlDbContext`)
  instead of re-registering it. The context is reachable through an internal
  `WriteDbContextAccessor`, which is what `EfCoreRepository` takes — **not** a bare
  `DbContext`. A bounded context owns a write *and* a read database (ADR-0021), so
  the unqualified `DbContext` key belonged to the write context by convention only:
  a host registering its read context under that key decided by registration order
  which database the repository wrote to, silently. The accessor is filled by this
  registration and used by nobody else, so the question no longer arises — and
  unlike a marker interface it puts no requirement on the host's own context type.
- `UseMartenEventSourcing` configures Marten with **string stream identities**
  (`StreamIdentity.AsString`, required by the `EntityKeyFormatter` stream-key
  scheme, §3) and **lightweight sessions** (no identity-map/change-tracking
  overhead — appends are staged explicitly by the repository), and integrates
  the session with Wolverine so it can be enrolled in the transactional outbox.
- `UseWolverineMessaging(rabbitMqUri, exchangeName, contextName)` takes the three
  transport coordinates together (ADR-0027, ADR-0023 amendment 2026-08-05): the broker
  URI (typically the Aspire-provided connection string), the platform exchange name,
  and the host's own bounded-context name. The exchange name belongs to the product,
  not to Building Blocks — VitalSync defines it once in `VitalSync.ServiceDefaults` and
  every host passes that constant. `contextName` must be a single lower-case kebab-case
  word; a value containing a dot is rejected, because it is almost certainly the
  exchange name in the wrong position. The call **requires a persistence selection**:
  `AddBuildingBlocks` throws when a broker URI was given without
  `UseEfCorePersistence` or `UseMartenEventSourcing`, because the durable sending
  endpoint has no message store to write to and Wolverine would degrade quietly to
  a host that only looks durable (ADR-0023 amendment 2026-08-04). The check runs
  after the whole options lambda, so either call order is accepted.
- Connection strings follow the write/read pair naming of ADR-0021 — the **Aspire
  resource name in kebab-case**, e.g. `nutrition-write` / `nutrition-read`. The
  service host uses the same names for its readiness checks, so a rename in the
  AppHost has exactly one spelling to follow.
- The exact API shape of the options builder is illustrative and may evolve
  during implementation; the registration responsibilities above are
  normative.
- `AddHandlersFrom` is **idempotent for multi-handler contracts**
  (`IProjectionHandler<>`, `IIntegrationEventMapper`): scanning the same assembly
  twice never registers a projection or mapper twice, so a projection runs at most
  once per event, while two *different* handlers for the same event both stay
  registered. For **single-handler contracts** (`ICommandHandler<>`,
  `ICommandHandler<,>`, `IQueryHandler<,>`) it enforces exactly one handler:
  discovering two *different* handlers for the same command or query throws at
  registration (naming both types) rather than letting the container silently pick
  one. A `ReflectionTypeLoadException` while scanning is rewrapped into a clear
  `InvalidOperationException` (usually a missing package reference).
- **Startup handler validation always runs**: `AddBuildingBlocks` registers a
  hosted service that, when the host starts, resolves the handler for every
  `ICommand`/`ICommand<>`/`IQuery<>` implementation found in the scanned
  assemblies and fails the host with every unresolvable request type named —
  turning "no service registered" production errors into fail-fast startup
  errors. The service host needs no extra wiring, and there is no switch to turn
  it off (ADR-0027 amendment 2026-08-05): every one of these checks exists because
  the failure it catches is otherwise silent, so an opt-out would only restore a
  silent failure. The check runs only inside a real host (`IHostedService`), so
  bare service providers in unit tests are unaffected.

**Every host whose selection flows through the outbox must additionally run
Wolverine** — and since the ADR-0027 amendment of 2026-08-03 the host does not
even issue that call. Registering through the **host-builder overload** hands
both steps to Building Blocks:

```csharp
builder.AddBuildingBlocks(options =>
{
    options.AddHandlersFrom(typeof(CreateRecipe).Assembly);
    options.UseEfCorePersistence<NutritionWriteDbContext>(writeConnectionString);
    options.UseWolverineMessaging(rabbitMqUri, exchangeName, "nutrition");
});
```

- It calls `UseWolverine` when the selection needs a runtime and applies the EF
  Core outbox against the **same** connection string `UseEfCorePersistence`
  recorded, so outbox rows and aggregate state cannot end up in different
  databases. Wolverine allows exactly one `UseWolverine`, so host-specific
  transport settings belong in the overload's optional
  `Action<WolverineOptions>` parameter rather than a second call.
- The remaining defaults come from a registered `IWolverineExtension`
  (`BuildingBlocksWolverineExtension`, ADR-0027): domain-event routing whenever
  a persistence style was selected, and the RabbitMQ transport, retry, and
  dead-letter defaults when messaging was selected. The underlying `Apply*`
  methods are `internal`; hosts have no Wolverine configuration surface and
  cannot forget or mismatch a call.
- This is the **only** way to obtain the EF Core outbox: the former public
  `UseBuildingBlocksEfCorePersistence(cs)` is gone, so no host can point the
  message store at a second database. The `IServiceCollection` overload still
  registers handlers, Marten, and messaging for hosts that wire Wolverine
  themselves — but a **state-stored** context must register through the host
  builder.
- **Startup Wolverine validation always runs** (ADR-0027): when a selected
  capability requires Wolverine but no runtime is registered — reachable only on
  that manual path now — a hosted service fails the host at startup with an
  actionable message, instead of surfacing as a missing outbox on the first
  commit in production.
- **Startup subscription validation always runs** when the host subscribed
  (ADR-0023 amendment 2026-08-05): once Wolverine has compiled its handler graph,
  every integration event handled by the declared consumer assembly must be matched
  by at least one bound topic pattern, and none of them may belong to the host's own
  context. Both cases fail the host, naming the type, its topic and the bound
  patterns. The reverse direction — a pattern with no matching contract — is
  deliberately not checked: binding ahead of an upstream context that does not exist
  yet is legitimate.
- A service that selects **no** persistence and **no** messaging needs no
  Wolverine at all; a service with purely in-context projections needs no
  RabbitMQ (the domain-event route is a local durable queue).

### Inside the composition root

`BuildingBlocksOptions` is the fluent surface a host sees and nothing more. Each of its
methods validates its own arguments and delegates to one internal collaborator in
`DependencyInjection/Registration/`:

| Collaborator          | Owns                                                                    |
| --------------------- | ----------------------------------------------------------------------- |
| `HandlerRegistrar`    | assembly scanning, handler/mapper/projection registration, behaviors    |
| `PersistenceRegistrar`| EF Core and Marten wiring — the only place that knows either            |
| `MessagingRegistrar`  | the RabbitMQ selection and the single subscription                      |
| `DomainEventCatalog`  | the domain-event assemblies and the registry they are frozen into       |

The split is what keeps the options type honest: it no longer references EF Core,
Npgsql, Marten, or Wolverine at all, so a third-party concern has exactly one file it
can be changed in. The public API is unchanged — the fluent chain still reads
`options.AddHandlersFrom(...).UseEfCorePersistence<T>(...)`, and each method still
returns `this`.

The work itself lives in the internal `BuildingBlocksComposition`, which runs six
named phases in a fixed order:

| Phase                    | Does                                                                    |
| ------------------------ | ----------------------------------------------------------------------- |
| `EnsureSingleCall`       | rejects a second `AddBuildingBlocks` on the same service collection      |
| `Configure`              | runs the caller's options lambda                                        |
| `Validate`               | rejects contradictory selections and **freezes** the domain-event registry |
| `RegisterCore`           | dispatcher, behaviors, persistence, publisher, messaging, clock          |
| `ValidateBehaviorOrders` | rejects an `IPipelineBehavior<,>` that was registered without an order   |
| `RegisterStartupChecks`  | the hosted services from `DependencyInjection/Validation/`               |

The order is load-bearing rather than cosmetic. `Validate` materialises the
`DomainEventTypeRegistry`, which is what makes `AddDomainEventsFrom` a
composition-time decision instead of a mutable setting — every later phase reads a
frozen set. `ValidateBehaviorOrders` runs after `RegisterCore` because that is where
the built-in behaviors enter the registry. `RegisterStartupChecks` runs last because
it registers the runner that drives the checks, and the runner must be in the
container after everything it inspects.

### `AddBuildingBlocks` is called exactly once

A host has one composition root, and three of the objects registered here are a
**single shared instance** that the options lambda writes into: the
`PipelineBehaviorRegistry`, the `WolverineWiringSettings`, and the
`DomainEventTypeRegistry`. They used to be registered with `TryAddSingleton`, which
made a second call the worst kind of bug — it succeeded. The first instance stayed in
the container, the second call filled a fresh one nobody resolves, and the result was
three silent failures at once: its behaviors ran at order `0`, its persistence and
messaging selection was ignored, and its `[EventName]` names were missing at the first
commit.

`EnsureSingleCall` therefore drops a marker descriptor into the service collection and
throws when it is already there; both public entry points funnel through
`AddBuildingBlocksCore`, so there is one place to guard. The three shared objects are
registered with plain `AddSingleton` afterwards — a foreign registration of the same
type should collide loudly, not win quietly. Nothing is lost by the restriction: a
bounded context has one write database (ADR-0021), Wolverine permits one
`UseWolverine`, and `AddDomainEventsFrom` is frozen by `Validate` anyway. Every
selection belongs in the same callback.

### A behavior without an order is a registration error

`PipelineBehaviorRegistry.GetOrder` used to return `0` for an unknown behavior — which
is exactly `LoggingBehaviorOrder`, so a behavior added straight to the
`IServiceCollection` silently shared a slot with the logging behavior and the
canonical order became unpredictable. It now throws, and `ValidateBehaviorOrders`
moves that failure from the first dispatched request to host start: it scans the
service collection for `IPipelineBehavior<,>` descriptors and rejects any whose
implementation type the registry does not know — including a factory-registered one,
whose implementation type is not inspectable at all. The fix is always the same, and
the message says so: register it with `options.AddPipelineBehavior(typeof(X<,>), order)`.

### Start-up checks: one contract, two phases

Every start-up check implements the internal `IStartupCheck`:

```csharp
internal interface IStartupCheck
{
    StartupPhase Phase { get; }

    Task RunAsync(CancellationToken cancellationToken);
}
```

Most checks do no I/O at all. Those derive from `SynchronousStartupCheck`, which implements
the async member over a `protected abstract void Run()` — six bodies returning
`Task.CompletedTask` would trip `IDE0046` under warnings-as-errors and, worse, hide which
checks actually wait on something. Only a check that genuinely reaches out —
`InfrastructurePresenceCheck` asking the message store whether its tables exist —
implements `IStartupCheck` directly.

A single `StartupCheckRunner` — the only `IHostedLifecycleService` Building Blocks
registers — resolves `IEnumerable<IStartupCheck>` and runs the
`BeforeHostedServicesStart` checks in `StartAsync` and the
`AfterHostedServicesStarted` checks in `StartedAsync`.

| Check                              | Phase    | Fails when                                                     |
| ---------------------------------- | -------- | -------------------------------------------------------------- |
| `HandlerRegistrationCheck`         | before   | a scanned command/query has no handler, or two result contracts |
| `WolverineRuntimeCheck`            | before   | a capability needs Wolverine and no runtime is registered       |
| `AggregateStateModelCheck<T>`      | before   | an aggregate state maps a forbidden navigation or key, or leaves a stored field name to convention |
| `UnitOfWorkPresenceCheck`          | before   | commands are scanned, nothing commits them, and nobody said so  |
| `IntegrationEventSubscriptionCheck`| after    | a handled integration event matches no bound pattern, or is own |
| `IntegrationEventMapperCheck`      | before   | mappers are registered and every event they produce would reach the null sink |
| `InfrastructurePresenceCheck`      | after    | the host does not provision and the message store's tables are missing |
| `BrokerTopologyCheck`              | before   | the host does not provision and the exchange or the subscriber queue is missing |

**Only the phase is load-bearing, not the registration order.** The checks are pure
readers — with the single exception of `MartenSchemaProvisioner` below, none mutates
state another one reads — so their relative sequence decides
only which message a broken host sees first. What *is* guaranteed, and what
`IntegrationEventSubscriptionCheck` depends on, is the .NET host's three-pass start:
every `StartAsync` completes before any `StartedAsync` begins. Wolverine compiles its
handler graph in its own `StartAsync`, so an `AfterHostedServicesStarted` check sees
it regardless of who was registered first. `StartupCheckRunnerTests` nails that
guarantee down with a real host and a hosted service registered *after* the runner.

Two consequences for authoring:

- **A check registers unconditionally and guards itself.** `WolverineRuntimeCheck` and
  `IntegrationEventSubscriptionCheck` take `WolverineWiringSettings` and return early
  when the capability was not selected. Conditional registration would make "is this
  check even present?" a second thing to reason about.
- **`UnitOfWorkPresenceCheck` probes the built container**, by resolving `IUnitOfWork`
  from a scope rather than reading the `IServiceCollection` at composition time. A host
  that registers `IUnitOfWork` *after* `AddBuildingBlocks` used to be flagged wrongly;
  now it is not.
- **A check asks about the effect, not about the selection.** `IntegrationEventMapperCheck`
  fires when a mapper is registered and the resolved `IIntegrationEventSinkFactory` is
  still the null one — deliberately *not* when `UseWolverineMessaging` was skipped. Both
  phrasings catch the real mistake, but only the first lets a host supply its own sink
  factory, which the delivery tests do. It is the same shape as `UnitOfWorkPresenceCheck`
  asking whether `IUnitOfWork` is the `NullUnitOfWork` rather than whether a persistence
  strategy was chosen.

A new check is a new `IStartupCheck` plus one `TryAddEnumerable` line — never another
hosted service.

### Provisioning is a role, not a start-up side effect

ADR-0037 turns "create the schema, the message-store tables and the broker topology" into
a selected role: `options.ProvisionInfrastructure(InfrastructureProvisioning.AtStartup)`.
Exactly one host per bounded context — the migration worker — selects it; every other host
keeps the default `Never` and fails at start when what it needs is missing.

One value drives three settings, because the three effects are one decision:

| Component         | `Never`                                              | `AtStartup`                 |
| ----------------- | ---------------------------------------------------- | --------------------------- |
| Marten            | `AutoCreateSchemaObjects = AutoCreate.None`          | `AutoCreate.CreateOrUpdate` |
| Wolverine storage | `AutoBuildMessageStorageOnStartup = AutoCreate.None` | `AutoCreate.CreateOrUpdate` |
| RabbitMQ          | nothing is declared                                  | `AutoProvision()`           |

Wolverine's two switches act at its own start, but Marten applies a configured change
lazily on first use, so `AtStartup` adds one step of our own: `MartenSchemaProvisioner` in
`DependencyInjection/Provisioning/`, an `IStartupCheck` in the `BeforeHostedServicesStart`
phase calling `ApplyAllConfiguredChangesToDatabaseAsync`. It is the **only** writer among
the start-up steps, which is what keeps "the phase is load-bearing, not the order" true —
a second writer would need a real ordering mechanism instead.

With provisioning off, a missing prerequisite must fail the start rather than the first
request, and two checks cover the two stores:

- `InfrastructurePresenceCheck` (`AfterHostedServicesStarted`) asserts Wolverine's message
  storage exists.
- `BrokerTopologyCheck` (`BeforeHostedServicesStart`) declares the exchange and the
  subscriber queue passively on a connection of its own.

Three traps worth knowing:

- **`AddResourceSetupOnStartup` is deliberately not used**, although it looks like exactly
  this feature. It runs as a hosted service concurrently with Wolverine's own start and
  opens RabbitMQ channels while Wolverine is opening its own; Wolverine 6.23 dereferences a
  channel that can be null at that moment (`RabbitMqListener.CreateAsync`) and a dozen
  messaging tests fail with a bare `NullReferenceException`.
- **`DeclarePassive` on an exchange does nothing here**, which is why `BrokerTopologyCheck`
  exists (ADR-0037 amendment). Wolverine reads that flag inside `DeclareAsync`, and both
  `RabbitMqExchange.InitializeAsync` and `RabbitMqQueue.InitializeAsync` reach it only
  under `if (_parent.AutoProvision …)`. With provisioning off nothing is declared at all,
  so setting the flag was dead code that read like a guarantee. Measured: a missing queue
  failed the start with a bare AMQP 404, and a **missing exchange let the host start and
  every publish return successfully while nothing arrived**.
- **A test that asserts the flag confirms nothing.** The first implementation had exactly
  such a test and it was green throughout. Broker behaviour is pinned against a real
  broker in `BrokerTopologyCheckTests`.

An integration test that owns its own PostgreSQL or RabbitMQ container **is** the
provisioning host for that container and must say so — every container-backed test host
here selects `AtStartup` right after its persistence call.

### Committing nothing is a choice, not a default

`UnitOfWorkBehavior` used to take `IUnitOfWork? unitOfWork = null` and skip the commit
when nothing was registered. The crash it originally fixed was real, but the state it
left behind has the worst possible failure shape: **the command reports success and the
data is gone**, with a single `Information` log at start as the only evidence. Nobody
reads that log in production.

The dependency is therefore non-optional. Building Blocks registers a `NullUnitOfWork`
fallback in `RegisterCore` (`TryAddScoped`, so a real one always wins) purely so the
pipeline resolves, and `UnitOfWorkPresenceCheck` decides at start whether reaching that
fallback is acceptable:

| Situation                                            | Outcome                        |
| ---------------------------------------------------- | ------------------------------ |
| a persistence strategy was selected                  | passes — the registrar wired a real `IUnitOfWork` |
| `UseNoPersistence()` was selected                    | passes, logs the deliberate choice |
| the host registered its own `IUnitOfWork`            | passes                         |
| no commands in the scanned assemblies                | passes — there is nothing to commit |
| commands are scanned and only the fallback resolves  | **throws**, naming the commands |

`UseNoPersistence()` is a positive selection on `PersistenceChoice`, not an opt-out
flag: it is mutually exclusive with `UseEfCorePersistence`/`UseMartenEventSourcing`
(combining them throws) and it requires no message store, no Wolverine, and no domain
event assembly. `PersistenceChoice` gains a fourth case for it, which is why the choice
now distinguishes `IsChosen` (something was said) from `IsSelected` (a real store
exists — the fact that drives the outbox, domain-event routing and Wolverine).

The last row is the point of the whole change: a gateway, a test, or a query-only host
still works untouched, while a real service host cannot reach the silent path without
writing `UseNoPersistence()` in its own composition root.

### A note on folder names

There is no `Persistence/Marten/` or `DependencyInjection/Wolverine/` folder, and there
must not be. C# resolves a name against the enclosing namespaces first, so inside
`BuildingBlocks.Infrastructure.Persistence.Marten` a plain `using Marten;` binds to the
own namespace and every Marten type stops resolving. The names `StateStored`,
`EventSourced`, and `Wiring` avoid the collision and happen to describe the *role*
rather than the vendor, which is the better name anyway.

### A note on type names

The same caution applies one level down, to type names that a *vendor* already uses for
something else. The dispatcher is `RequestSender`, not `Sender`, and the domain-event
publication step is `DomainEventPublisher`, not `Publisher`. Both short names were
already taken by Wolverine for a different concept — `ISender` is a transport endpoint
there, and "publish" means putting a message on the broker — so in any file that sees
both, the bare name forced a second look. Neither rename fixes a bug the compiler would
have caught; they buy the reader the right guess on the first try. The rule: **do not
name an Infrastructure type with a bare noun that Wolverine, Marten, or EF Core also
uses.** Qualify it with what it actually operates on.

The rule also covers collisions with **our own** types: the helper that derives a routing
key from an event type is `TopicResolver`, not `IntegrationEventTopic`, because
`[IntegrationEventTopic]` in `BuildingBlocks.Application` is the attribute it reads. A
class and an attribute that differ only by the compiler-elided `Attribute` suffix both
compile and both read the same in a call site, which is precisely the problem.

### A note on names found in ADRs

The four Wolverine wiring extensions are `ApplyBuildingBlocksIdempotencyWindow`,
`ApplyBuildingBlocksDomainEventRouting`, `ApplyBuildingBlocksMessagingDefaults`, and
`ApplyBuildingBlocksSubscription` — plural, matching the assembly. **ADR-0023 and ADR-0027
name them in the singular**, and were deliberately left untouched when they were renamed.

That is the general rule, not an exception: **a code name in an ADR is historical.** An ADR
records a decision together with the vocabulary that existed when it was accepted; it is not
reference documentation and is not kept name-current. ADR-0027 has named
`ApplyBuildingBlockEfCoreOutbox` — a method since deleted — for some time already. When
a name in an ADR does not resolve, this file, `WalkingSkeleton.md`, and the two instruction
files are the ones that are kept current; look there.

## 8. Clock (`IClock` implementation)

`IClock` is the narrow time port declared in `BuildingBlocks.Domain` ("the
domain only ever needs *now*"). Infrastructure ships the single default
implementation so no service has to write one:

```csharp
internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset Now => timeProvider.GetUtcNow().ToUniversalTime();
}
```

- **UTC, always.** Everything that is persisted, projected, or transported
  across a service boundary is UTC; time zones are a presentation concern and
  belong in the frontend. The result is normalized to a zero offset, so the
  promise holds even for a `TimeProvider` that reports the current instant with
  an offset of its own.
- **Built on `TimeProvider`**, not parallel to it. `IClock` stays the narrow
  domain-facing port; `TimeProvider` — the broad infrastructure abstraction
  including timers and time zones — never reaches the domain. Tests get the
  full, proven time control of `FakeTimeProvider`
  (`Microsoft.Extensions.TimeProvider.Testing`) instead of every test project
  writing its own fake clock.
- **Registered with `TryAdd`** (`TimeProvider.System` and `IClock`), so a host
  or test can substitute either one. Singleton, because both are stateless.

## 9. Tracing (`ActivitySource`)

Three paths carry a span, and each answers a different question. The source is named
**`BuildingBlocks`** and its version is the assembly's `AssemblyInformationalVersion`; every tag
starts with `buildingblocks.`, matching the existing message header
`buildingblocks.source-context`.

| Path                   | Span                    | Answers                                                     |
| ---------------------- | ----------------------- | ----------------------------------------------------------- |
| `RequestSender`        | `Send {RequestName}`    | which command or query, how long, with what outcome         |
| `ProjectionRunner`     | `Project {HandlerName}` | **which** projection handler, per handler                   |
| `DomainEventPublisher` | `Publish {EventName}`   | projections vs. broker, and how many integration events went out |

The projection spans nest inside the publish span, so a trace shows directly how a commit's
after-work splits between read models and the transport.

Four rules hold here and are worth keeping:

- **The name must not say `vitalsync`.** ADR-0018 guarantees the string does not occur anywhere
  under `BuildingBlocks/src`. The source name is therefore `BuildingBlocks`, and the host
  registers it as a literal in `AspireExtensions.cs` — exactly like `Npgsql`, `Wolverine`, and
  `Marten`. A typed constant would need a `ProjectReference` from `VitalSync.ServiceDefaults` to
  `BuildingBlocks.Infrastructure`, which has none today, and would drag Marten, Wolverine, and
  EF Core into every host's dependency tree. Two tests pin the literal from opposite ends:
  `OpenTelemetryConfigurationTests` proves the host listens to that name,
  `TracingTests` proves Building Blocks emits under it.
- **A domain failure is not an error span.** `Result.Failed` sets `ActivityStatusCode.Ok` and
  names the categories in a tag; only a propagating exception sets `Error`. This mirrors the
  logging rule (expected domain errors log as `Warning`) and keeps validation noise out of
  error-rate dashboards.
- **A null guard stays synchronous.** `RequestSender.SendAsync` is not `async` — the
  `ArgumentNullException.ThrowIfNull` must fire at the call, not at the `await`. The dispatch
  call moves into a private `async` helper instead.
- **No listener, no cost.** The interpolated span name is evaluated before `StartActivity` can
  return `null`, so all three sites check `Source.HasListeners()` first and otherwise run the
  unchanged path.

## What deliberately does NOT live here

- **Read models, projection handlers, queries** — per service (ADR-0022).
- **Domain-to-integration-event translation maps** — per service.
- **HTTP/gRPC status mapping** — the BFF and service hosts own transport
  mapping of `FailureCategory` (see
  [BuildingBlocks.Application](./building-blocks-application.md)).
- **Any use-case contract** — contracts belong in the innermost layer that
  consumes them (ADR-0024).
- **MassTransit** — superseded by Wolverine; must not be reintroduced
  (ADR-0023).

## Third-party dependencies (exhaustive)

| Package                                                                          | Used for                                           |
| -------------------------------------------------------------------------------- | -------------------------------------------------- |
| `Microsoft.EntityFrameworkCore` + Npgsql provider                                | State-stored persistence (ADR-0020)                |
| `Marten`                                                                         | Event store, raw stream access (ADR-0019)          |
| `WolverineFx.RabbitMQ`                                                           | Integration-event transport (ADR-0023)             |
| `WolverineFx.Marten`                                                             | Native transactional outbox for Marten (ADR-0023)  |
| `WolverineFx.EntityFrameworkCore`                                                | Native transactional outbox for EF Core (ADR-0023) |
| `Microsoft.Extensions.DependencyInjection.Abstractions` / `Logging.Abstractions` | DI wiring, logging behavior                        |

Adding any further third-party dependency to the platform requires it to land
**here** — and nowhere else.

## Testing

`BuildingBlocks.Infrastructure.Tests` mirrors this project. Tests use xUnit
(built-in asserts), NSubstitute, and EF Core InMemory where applicable
(ADR-0014). Dispatching, DI wiring, failure translation, serialization, and
entity-key mapping are covered by fast in-memory tests against the real `RequestSender`
and a DI container. Marten optimistic concurrency and strongly-typed key
persistence are covered by integration tests against a disposable PostgreSQL
instance via Testcontainers (skipped automatically when Docker is unavailable).
A small set of architecture tests enforces the layer-dependency rules, and
`PublicSurfaceTests` pins the exported types of this assembly against the table in
[Public surface](#public-surface): adding a `public` type fails the test until it is
listed with a reason, which keeps "internal by default" from eroding one convenient
exception at a time. See [Testing strategy](./testing-strategy.md).
