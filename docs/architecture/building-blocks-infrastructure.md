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

## Capabilities (internal organization)

`Infrastructure` is deliberately one package (ADR-0018), organized internally by
capability. Top-level namespaces/folders:

| Folder                 | Capability                                                     |
| ---------------------- | -------------------------------------------------------------- |
| `Dispatching/`         | DI-based `ISender` implementation + pipeline behaviors         |
| `Persistence/`         | Unit of work, EF Core generic repository, Marten ES repository |
| `Events/`              | Domain-event publisher, projection runner                      |
| `Messaging/`           | Wolverine/RabbitMQ integration-event transport                 |
| `Time/`                | `IClock` implementation on top of `TimeProvider`               |
| `DependencyInjection/` | `IServiceCollection` registration extensions                   |

---

## 1. CQRS dispatcher (`ISender` implementation)

Implements the `ISender` contract from `BuildingBlocks.Application`
(ADR-0015):

- Resolves the single matching `ICommandHandler<...>` / `IQueryHandler<...>`
  from the DI container.
- Wraps the handler in the registered `IPipelineBehavior<,>` chain.
- **Behavior ordering is an explicit numeric order** — each behavior is registered
  with an `order`, and the sender wraps them by ascending order (lower orders wrap
  further out and execute earlier); no attribute- or convention-based ordering, and
  no reliance on registration call order. Hosts add their own behaviors at a chosen
  position via `BuildingBlocksOptions.AddPipelineBehavior(type, order)`.
- No reflection-heavy scanning at dispatch time; the closed-generic dispatcher is
  cached **per request/result type pair** after first use (the result type is part
  of the cache key, so a request type exposing two result contracts still resolves
  the correct dispatcher — the startup validator rejects such types in scanned
  assemblies anyway, since a command/query has exactly one result type).

### Pipeline behaviors shipped here

| Behavior                    | Order | Responsibility |
| --------------------------- | ----- | -------------- |
| `LoggingBehavior`           | `0` (outermost) | Structured logging of request name, outcome (success/failure categories), and duration. Never logs payload contents by default. Being outermost, translated failures (expected domain errors, concurrency conflicts) are logged at `Warning`, while only genuinely unexpected exceptions are logged at `Error` (faulted) and rethrown. |
| `ExceptionToResultBehavior` | `100` | Translates `DomainValidationException` / `BusinessRuleViolationException` into `Result.Failure` (ADR-0017), before logging sees the outcome and before any transaction is opened. Unexpected exceptions pass through untouched. |
| `UnitOfWorkBehavior`        | `300` (innermost) | Commits the unit of work when a **command** completes successfully — including the atomic outbox write (see §2/§4). Queries and failed results are unaffected: nothing is committed, and query-only hosts need no `IUnitOfWork` registration. The unit of work is an **optional** dependency (nullable constructor injection): hosts without configured persistence (handler tests, gateway/facade services, hosts with their own persistence) dispatch commands as a pass-through without commit; `AddBuildingBlocks` logs a startup notice when no `IUnitOfWork` is registered so the no-op is visible. Translates the store's optimistic-concurrency exceptions raised on commit (Marten's `ConcurrencyException`, EF Core's `DbUpdateConcurrencyException`) into a `Failure` with category `Conflict`. |

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
public interface IUnitOfWork   // contract lives in Application (ADR-0024)
{
    Task CommitAsync(CancellationToken ct);
}
```

- **One unit of work per command dispatch**, owned by `UnitOfWorkBehavior`.
- On commit, in a **single write-database transaction**:
    1. persist aggregate changes (EF Core `SaveChanges` or Marten stream append);
    2. collect the aggregates' uncommitted domain events;
    3. stamp each event's `OccurredAt` with the transaction's commit time (one
       `IClock.Now` value shared by all events; already-stamped events are left
       untouched, so replayed events keep their original time);
    4. write those events to the **transactional outbox** in the write database
       (ADR-0022, ADR-0023) — atomically with the state change;
    5. clear the aggregates' event collections.
- **Optimistic-concurrency conflicts surface at commit, not before.** Both
  stores only detect a version conflict when the changes are flushed
  (`SaveChanges` / `SaveChangesAsync`); repositories only hand aggregates to
  the tracker and therefore cannot observe these exceptions. The commit path —
  via the `UnitOfWorkBehavior` (§1) — translates them into a `Conflict`
  failure.
- Two implementations, one per persistence style, sharing the same behavior:
  an EF Core–backed unit of work and a Marten-session–backed unit of work.
  Wolverine's native Marten/EF Core integration provides the shared
  transaction + outbox enlistment (ADR-0023).

## 3. Generic repositories

Both persistence styles implement the **same** `IRepository<,>` contract from
`Application` ([ADR-0026](./decisions/0026-single-repository-contract.md)):

```csharp
public interface IRepository<TAggregate, in TKey>   // contract lives in Application (ADR-0024)
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

### EF Core repository (state-stored contexts, ADR-0020)

- Strongly typed identifiers (ADR-0005) are mapped via EF Core value converters
  provided here as reusable conventions.
- Change tracking comes from the `DbContext`; the unit of work collects tracked
  aggregates' events from the change tracker at commit.

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
  serialization scheme; the envelope carries the event's stable `EventId` and
  its concrete type for faithful round-tripping.
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
      via the event's stable `EventId` (generated once when the event is
      raised) as the last-processed marker — this works identically for
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
- Retries and dead-lettering are configured here with sane, overridable
  defaults.
- **Routing.** Connecting the transport moves nothing on its own — Wolverine
  routes only what a routing rule matches, and `PublishAsync` silently discards
  an unroutable message. `UseWolverineMessaging` therefore installs the rule
  `MessagesImplementing<IIntegrationEvent>().ToRabbitTopics("vitalsync.integration-events")`.
  Matching the marker rather than all messages is load-bearing: `DomainEventEnvelope`
  does not implement `IIntegrationEvent` and so can never be routed onto the
  broker (ADR-0022/0023, pinned by `IntegrationEventRoutingTests`). Each
  integration event supplies its routing key via a mandatory
  `[Topic("<context>.<event>")]` attribute.
- **Consumer side not included.** Queue declaration, binding, and listening are
  owned by the subscribing service; this package wires the publishing half only.

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
    options.UseEfCorePersistence<NutritionWriteDbContext>(writeConnectionString);   // or:
    options.UseMartenEventSourcing(writeConnectionString);
    options.UseWolverineMessaging(rabbitMqUri);
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
  instead of re-registering it.
- `UseMartenEventSourcing` configures Marten with **string stream identities**
  (`StreamIdentity.AsString`, required by the `EntityKeyFormatter` stream-key
  scheme, §3) and **lightweight sessions** (no identity-map/change-tracking
  overhead — appends are staged explicitly by the repository), and integrates
  the session with Wolverine so it can be enrolled in the transactional outbox.
- `UseWolverineMessaging(rabbitMqUri)` takes the broker URI (typically the
  Aspire-provided connection string) so the RabbitMQ defaults can be applied
  automatically (ADR-0027).
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
  `InvalidOperationException` (usually a missing package reference) (IMP-05).
- **Startup handler validation is on by default**: `AddBuildingBlocks` registers a
  hosted service that, when the host starts, resolves the handler for every
  `ICommand`/`ICommand<>`/`IQuery<>` implementation found in the scanned
  assemblies and fails the host with every unresolvable request type named —
  turning "no service registered" production errors into fail-fast startup
  errors. The service host needs no extra wiring; a host that intentionally
  registers handlers outside the assembly scan can opt out via
  `options.ValidateHandlersOnStart = false`. The check runs only inside a real
  host (`IHostedService`), so bare service providers in unit tests are
  unaffected (IMP-05).

**Every host whose selection flows through the outbox must additionally run
Wolverine** — and since the ADR-0027 amendment of 2026-08-03 the host does not
even issue that call. Registering through the **host-builder overload** hands
both steps to Building Blocks:

```csharp
builder.AddBuildingBlocks(options =>
{
    options.AddHandlersFrom(typeof(CreateRecipe).Assembly);
    options.UseEfCorePersistence<NutritionWriteDbContext>(writeConnectionString);
    options.UseWolverineMessaging(rabbitMqUri);
});
// no UseWolverine call - the write database was named once, above
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
- The `IServiceCollection` overload remains for tests and hosts that wire
  Wolverine themselves; those call `UseWolverine(opts =>
  opts.UseBuildingBlocksEfCorePersistence(writeConnectionString))` and are the
  only place that still names the write database twice.
- **Startup Wolverine validation is on by default** (ADR-0027): when a selected
  capability requires Wolverine but no runtime is registered — reachable only on
  that manual path now — a hosted service fails the host at startup with an
  actionable message, instead of surfacing as a missing outbox on the first
  commit in production. Opt out via `options.ValidateWolverineOnStart = false`.
- A service that selects **no** persistence and **no** messaging needs no
  Wolverine at all; a service with purely in-context projections needs no
  RabbitMQ (the domain-event route is a local durable queue).

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
entity-key mapping are covered by fast in-memory tests against the real `Sender`
and a DI container. Marten optimistic concurrency and strongly-typed key
persistence are covered by integration tests against a disposable PostgreSQL
instance via Testcontainers (skipped automatically when Docker is unavailable).
A small set of architecture tests enforces the layer-dependency rules. See
[Testing strategy](./testing-strategy.md).
