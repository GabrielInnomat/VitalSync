# BuildingBlocks.Infrastructure

`BuildingBlocks.Infrastructure` is the single outer layer of the Building Blocks
platform. It holds **all** reusable, framework-bound, third-party-backed
implementations that are still **independent of any VitalSync domain logic**
([ADR-0018](./decisions/0018-three-building-block-packages.md)). It depends on
`BuildingBlocks.Domain` and `BuildingBlocks.Application` and is the **only**
Building Block allowed to reference third-party packages.

> **Status: specification.** This package is not yet implemented. This document
> is the authoritative design it must be implemented against
> (documentation-first). Where the implementation and this document diverge,
> this document wins until amended.

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
  the innermost layer that consumes it (ADR-0024) — never here.
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
| `Dispatching/`         | DI-based `ISender` implementation + pipeline behaviors          |
| `Persistence/`         | Unit of work, EF Core generic repository, Marten ES repository  |
| `Events/`              | Domain-event publisher, projection runner                       |
| `Messaging/`           | Wolverine/RabbitMQ integration-event transport                  |
| `DependencyInjection/` | `IServiceCollection` registration extensions                    |

---

## 1. CQRS dispatcher (`ISender` implementation)

Implements the `ISender` contract from `BuildingBlocks.Application`
(ADR-0015):

- Resolves the single matching `ICommandHandler<...>` / `IQueryHandler<...>`
  from the DI container.
- Wraps the handler in the registered `IPipelineBehavior<,>` chain.
- **Behavior ordering is explicit registration order** — behaviors execute in
  the order they are added to DI; no attribute- or convention-based ordering.
- No reflection-heavy scanning at dispatch time; handler resolution should be
  cached (e.g. per closed generic type) after first use.

### Pipeline behaviors shipped here

| Behavior                    | Position             | Responsibility                                                                                                                                             |
| --------------------------- | -------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ExceptionToResultBehavior` | **First**            | Translates `DomainValidationException` / `BusinessRuleViolationException` into `Result.Failure` (ADR-0017). Unexpected exceptions pass through untouched.   |
| `LoggingBehavior`           | Second               | Structured logging of request name, outcome (success/failure categories), and duration. Never logs payload contents by default.                             |
| `UnitOfWorkBehavior`        | Last (commands only) | Begins the unit of work, invokes the handler, and commits on success — including the atomic outbox write (see §2/§4). Rolls back on failure. Queries bypass it. |

The canonical registration order is therefore:
`ExceptionToResult → Logging → UnitOfWork → handler`.

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
    3. write those events to the **transactional outbox** in the write database
       (ADR-0022, ADR-0023) — atomically with the state change;
    4. clear the aggregates' event collections.
- Two implementations, one per persistence style, sharing the same behavior:
  an EF Core–backed unit of work and a Marten-session–backed unit of work.
  Wolverine's native Marten/EF Core integration provides the shared
  transaction + outbox enlistment (ADR-0023).

## 3. Generic repositories

### EF Core repository (state-stored contexts, ADR-0020)

```csharp
public interface IRepository<TAggregate, TKey>   // contract lives in Application (ADR-0024)
    where TAggregate : AggregateRoot<TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken ct);
    Task AddAsync(TAggregate aggregate, CancellationToken ct);
    void Remove(TAggregate aggregate);
}
```

- Works against the context's **write database** only (ADR-0021).
- Strongly typed identifiers (ADR-0005) are mapped via EF Core value converters
  provided here as reusable conventions.
- No `Update` method — aggregates are tracked; changes flow through the unit of
  work.
- **No query methods beyond `GetByIdAsync`.** Queries never go through
  repositories: the read side reads its own read database directly
  (ADR-0021/0022).

### Marten event-sourced repository (ADR-0019)

Roughly **one class**, using Marten as a **raw stream store**:

- **Load:** `FetchStream(id)` → fold via
  `((IEventSourcedAggregateRoot<TKey>)aggregate).LoadFromHistory(history)`.
  Marten's `Apply`-on-aggregate convention is **never** used (preserves
  ADR-0010 / ADR-0012).
- **Save:** append uncommitted domain events to the stream with expected-version
  optimistic concurrency asserted against the aggregate's `Version`. Marten's
  `ConcurrencyException` is translated into a `Failure` with category `Conflict`.
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
- **Dispatch:** after commit, Wolverine delivers each envelope to the single
  `DomainEventEnvelopeHandler` this package registers, which unwraps it and
  calls the **Publisher**. The Publisher dispatches the unwrapped event to:
    - **in-context projection handlers** (via the projection runner, §5), which
      update the context's **read database**;
    - the **integration-event path** (§6) for events selected for cross-service
      communication.
- Delivery is **at-least-once**: a failed dispatch is retried by Wolverine's
  own outbox, not by any custom drain loop.
- The envelope is routed to a durable, strictly sequential local queue, so a
  single aggregate's events cannot be reordered relative to one another by a
  crash-triggered redelivery (see §7,
  `WolverineOptionsExtensions.ApplyBuildingBlockDomainEventRouting`).

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

## 7. Dependency-injection wiring

A small set of `IServiceCollection` extensions is the package's public surface
for hosts, e.g.:

```csharp
services.AddBuildingBlocks(options =>
{
    options.AddCommandsAndQueriesFrom(typeof(SomeHandler).Assembly);
    options.UseEfCorePersistence<NutritionWriteDbContext>();   // or:
    options.UseMartenEventSourcing("ConnectionStrings:NutritionWrite");
    options.UseWolverineMessaging(rabbitMqConnectionName: "messaging");
});
```

- Registers `ISender`, the behaviors **in the canonical order** (§1), the unit
  of work, repositories, the Publisher/outbox, the projection runner, and the
  Wolverine transport.
- Connection strings follow the write/read pair naming of ADR-0021
  (e.g. `NutritionWrite` / `NutritionRead`).
- The exact API shape of the options builder is illustrative and may evolve
  during implementation; the registration responsibilities above are
  normative.

**Every host must additionally run Wolverine**, because domain events now flow
through Wolverine's own transactional outbox even for purely in-context
projections (§4), not only for integration events:

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.ApplyBuildingBlockDomainEventRouting();       // always required
    opts.ApplyBuildingBlockEfCoreOutbox();              // only if UseEfCorePersistence was selected
    opts.ApplyBuildingBlockMessagingDefaults(rabbitMqUri); // only if UseWolverineMessaging was selected
});
```

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

| Package                                                                           | Used for                                          |
| --------------------------------------------------------------------------------- | ------------------------------------------------- |
| `Microsoft.EntityFrameworkCore` + Npgsql provider                                   | State-stored persistence (ADR-0020)               |
| `Marten`                                                                            | Event store, raw stream access (ADR-0019)         |
| `WolverineFx.RabbitMQ`                                                              | Integration-event transport (ADR-0023)            |
| `WolverineFx.Marten`                                                                | Native transactional outbox for Marten (ADR-0023) |
| `WolverineFx.EntityFrameworkCore`                                                   | Native transactional outbox for EF Core (ADR-0023) |
| `Microsoft.Extensions.DependencyInjection.Abstractions` / `Logging.Abstractions`   | DI wiring, logging behavior                       |

Adding any further third-party dependency to the platform requires it to land
**here** — and nowhere else.

## Testing

`BuildingBlocks.Infrastructure.Tests` mirrors this project. Tests use xUnit
(built-in asserts), NSubstitute, and EF Core InMemory where applicable
(ADR-0014); Marten/Wolverine components are covered with integration tests
against disposable PostgreSQL/RabbitMQ instances. See
[Testing strategy](./testing-strategy.md).
