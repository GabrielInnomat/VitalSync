# CQRS & Event Sourcing

## CQRS (mandatory)

Every microservice implements **Command Query Responsibility Segregation (CQRS)** to separate write operations from read operations.

- **Commands** change state and express intent (e.g., _CreateRecipe_, _CompleteWorkoutSession_). They return a `Result` (success/failure), or a `Result<T>` when a value is needed — e.g. a **create** returns the new typed id.
- **Queries** read state and never mutate it; they return `Result<T>`.
- Commands and queries are handled by **dedicated handlers**.

```text
        write side                          read side
┌───────────────────────┐         ┌───────────────────────┐
│  Command ──► Handler   │         │  Query ──► Handler     │
│        │               │         │        │              │
│        ▼               │         │        ▼              │
│   Aggregate / Domain   │         │   Read model / store  │
└────────────────────────┘         └───────────────────────┘
```

The Application package provides the `ICommand`, `IQuery`, and corresponding handler abstractions, a hand-rolled dispatcher, and the `Result` / `Failure` model. Domain exceptions (`BusinessRuleViolationException`, `DomainValidationException`) are translated to `Result.Failed` by an Application pipeline behavior.

## Persistence strategy

Two complementary approaches are used:

1. **Traditional persistence (default).** Most contexts use **Entity Framework Core** to persist aggregate state directly.
2. **Event Sourcing (selective).** Where it provides **business value**, an aggregate's state is derived from an append-only stream of domain events instead of being stored directly.

> **Decision rule:** Event Sourcing is applied **only where it adds business value**. In all other cases, EF Core is used. The exact contexts that justify Event Sourcing are **to be determined** during the project.

> **The choice is per bounded context, not per aggregate.** A microservice hosts exactly one bounded context, and a bounded context uses **exactly one** persistence strategy — either all state-stored (EF Core) or all event-sourced (Marten), never both. The two stores live in separate databases (see [ADR-0020](./decisions/0020-postgresql-for-state-stored-contexts.md)), so a single commit cannot span them atomically. A context that appears to need both an event-sourced and a state-stored aggregate is a sign it is **cut wrong** and should be split into two bounded contexts, each in its own microservice with its own single strategy. `AddThessera` enforces this: selecting both `UseEfCoreStateStore<TContext>(…)` and `UseMartenEventStore(...)` for the same host **throws at startup**.

### When might Event Sourcing add value here?

These are **candidates to evaluate**, not decisions:

- **Workout session tracking** — a natural event stream (started, exercise logged, completed); full history may be valuable for analytics.
- **Nutrient intake over time** — append-only logging of consumed meals.

Contexts that are largely CRUD-shaped (e.g., managing the ingredient catalog) are likely better served by EF Core.

### Trade-offs to weigh during "analyze & challenge"

| Aspect                    | EF Core (state-stored) | Event Sourcing                                |
| ------------------------- | ---------------------- | --------------------------------------------- |
| Implementation complexity | Lower                  | Higher                                        |
| Full audit/history        | Not inherent           | Inherent                                      |
| Temporal queries / replay | Hard                   | Natural                                       |
| Read models               | Direct from tables     | Usually via projections                       |
| Operational overhead      | Lower                  | Higher (event store, projections, versioning) |

## Database & topology

Both persistence approaches run on **PostgreSQL** — the single relational engine
for the platform. State-stored contexts use EF Core (via the Npgsql provider) and
event-sourced contexts use Marten; both are PostgreSQL underneath.

**Each bounded context owns exactly two PostgreSQL databases: a write database and
a read database** (see [ADR-0021](./decisions/0021-write-read-database-pair-per-context.md)).

- The **write database** holds the authoritative state: EF Core tables for
  state-stored contexts, and the Marten event streams for event-sourced contexts.
- The **read database** holds **query-optimized read models** (projections), kept
  up to date from domain events after the write commits (see
  [ADR-0022](./decisions/0022-event-driven-read-models.md)). It is **derived and
  rebuildable** — never the system of record.

Contexts never share a database, and there are **no cross-database foreign keys,
joins, or transactions** — cross-context consistency is via integration events.
Today both databases of every context are hosted on **one shared PostgreSQL
server** (in Aspire: one server resource with **two `AddDatabase(...)` calls per
context**, e.g. `nutrition-write` and `nutrition-read`, each with its own named
connection string). Moving either database of a context onto its **own dedicated
server** later ("server per context") is an explicitly supported, non-breaking
migration: a connection-string change plus a data move, touching no
Domain/Application/Infrastructure code.

The event store and the state-stored store **never co-locate in the same
database**, even on the same server, so they can move and scale independently. See
[ADR-0020](./decisions/0020-postgresql-for-state-stored-contexts.md).

### Migrations and the design-time package

`Microsoft.EntityFrameworkCore.Design` belongs to the context's **MigrationService**,
never to its Infrastructure project. Infrastructure is referenced by the Api, the
MigrationService and the tests, so a design-time package placed there travels into
every one of them; the MigrationService is a leaf host, and only the Aspire AppHost
references it. The package is declared with `PrivateAssets="all"` and **without**
`IncludeAssets` — the frequently copied `IncludeAssets="runtime;build;native;contentfiles;analyzers"`
drops `compile` and the `IDesignTimeDbContextFactory` implementations then stop
compiling.

The migrations themselves stay in Infrastructure, next to the `DbContext` they
describe, so scaffolding names both projects:

```bash
dotnet ef migrations add AddSomething --context WidgetWriteDbContext \
  --project         samples/StateStored/VitalSync.Sample.StateStored.Infrastructure \
  --startup-project samples/StateStored/VitalSync.Sample.StateStored.MigrationService \
  --output-dir      Migrations/Write
```

The `IDesignTimeDbContextFactory` implementations live in the MigrationService and
stay `internal` — EF Core discovers them by reflection, and a `public` type in a
worker trips `CA1515`. They are **required**: without one, `dotnet ef` builds the
worker's own host, which reads its connection strings from Aspire configuration
that does not exist at design time. Each sample carries a `DesignTimePackageTests`
that fails once the package reappears in Infrastructure or loses `PrivateAssets`.

## Event store technology

When a context is event-sourced, its events are persisted in **Marten on
PostgreSQL**, used as a **raw event store**: the event-sourced repository in
`GaWeCodes.Thessera.Core` tracks the aggregates it hands out, and the unit
of work appends their uncommitted domain events to the stream at commit (with
optimistic concurrency asserted against the aggregate's `Version`); on load, the
repository fetches the raw stream and folds it through the aggregate's own
`LoadFromHistory`. Marten's convention-based `Apply`-on-aggregate aggregation is
**not** used, so the domain (ADR-0010 / ADR-0025) is untouched.

**Snapshotting is deferred but non-breaking:** because a Marten snapshot is a
separate document and the event schema is identical with or without snapshots,
snapshotting can be added per context later with **no event migration**.

Marten is MIT-licensed and runs on PostgreSQL, which has a first-party .NET Aspire
hosting integration. See [ADR-0019](./decisions/0019-event-store-technology-marten.md).

## Read models & projections

The read side of every context lives in its own **read database**
([ADR-0021](./decisions/0021-write-read-database-pair-per-context.md)) and is kept
up to date by an **event-driven, outbox-backed Publisher**
([ADR-0022](./decisions/0022-event-driven-read-models.md)), used **uniformly** for
event-sourced and state-stored contexts:

1. On command handling the aggregate raises **domain events** (ES: appended to the
   Marten stream; state-stored: collected on the aggregate).
2. In the **write transaction** those events are also written to a **transactional
   outbox** in the write database, so they are captured atomically with the change
   (no cross-database transaction is required).
3. After commit, the **Publisher** (in `GaWeCodes.Thessera.Core`) drains the
   outbox and dispatches each event to **in-context projection handlers** (which
   update the read database) and, where selected, to the **integration-event path**
   on RabbitMQ/Wolverine ([ADR-0023](./decisions/0023-wolverine-messaging-transport.md), which supersedes [ADR-0004](./decisions/0004-asynchronous-messaging-between-services.md)).
   The same outbox already required for integration events is reused.
4. An outbox entry is marked processed **only after** its handlers succeed, giving
   **at-least-once** delivery. On the integration-event path the promise extends past
   the handover to the broker: the sending endpoint is **durable**, so the event is
   recorded in `wolverine_outgoing_envelopes` before it is sent and reaches RabbitMQ
   as a **persistent** message on a **durable, quorum** topology — neither a broker
   restart nor a process crash between commit and acknowledgement loses it
   ([ADR-0023 amendment](./decisions/0023-wolverine-messaging-transport.md)).

Because delivery is at-least-once and the two databases are separate, read-model
updates are **eventually consistent** with writes, and projection handlers **must
be idempotent and per-aggregate order-aware** — the `DomainEventMetadata` a handler
receives carries the aggregate's `Version`, so the handler keeps that number on its
read model and ignores any event at or below it ([ADR-0030](./decisions/0030-persisted-names-and-aggregate-version.md)).
Read models are **domain-shaped and owned by each service** — not
a Thessera package; Infrastructure ships only the plumbing (Publisher, outbox,
dispatch loop, projection runner, transport). Read models are **rebuildable**, but by
different means per path: an event-sourced context replays its Marten stream, while a
state-stored context has no surviving event history — its outbox row is deleted once
delivered — and instead derives the read model again from the **current aggregate state**
([ADR-0036](./decisions/0036-state-stored-read-model-rebuild.md)).

### Rebuilding a read model

The live path is unchanged: a domain event goes through the outbox to the projection
handlers. Next to it sits a second, **explicitly invoked** path.

- A read model implements `IReadModelRebuilder<TAggregate, TKey>` (`ClearAsync` plus
  `RebuildAsync(aggregate)`) alongside its projection handlers. Several rebuilders per
  aggregate are allowed — one per read model. **The contract is the same on both
  persistence paths**; only the source of the aggregate differs.
- `StateStoredReadModelRebuildRunner<TContext>` clears once, streams every aggregate state out of the
  write database, rehydrates each aggregate and hands it to the rebuilders in batches. It
  is invoked by the context's migration worker behind a configuration switch, never
  automatically, and it throws when no rebuilder is registered.
- `EventSourcedReadModelRebuildRunner` does the same for a Marten context: it collects the
  distinct stream keys under the aggregate's `[AggregateName]` prefix, fetches each stream
  and folds it through `LoadFromHistory`. A rebuilt aggregate is indistinguishable from a
  loaded one and carries no uncommitted domain events.
- **Every field of a state-stored read model must be a function of the current aggregate
  state.** A rebuilder writes absolute values (`PartCount = parts.Count`), never
  increments. A field that needs history belongs in an event-sourced context.
- The handover back to live traffic needs nothing new: the rebuilder writes the aggregate's
  current `Version`, and the watermark check in every handler discards what is already
  contained.
- The rebuild does not run through the integration-event publisher, so it publishes no
  integration events.
- **A parity test is mandatory** where a context has both: one aggregate's events through
  the live projections, the same aggregate's final state through the rebuilders, both rows
  identical.

### One missing wire is an error, the other is not

A registered **integration-event mapper without a messaging transport** fails the host
at start (`IntegrationEventMapperCheck`). A mapper exists for exactly one purpose —
producing an event that leaves this context — so without a sink every event it makes is
handed to the null sink and dropped after a log warning while the commit reports
success. Nothing downstream distinguishes that from an upstream context that simply has
not published yet, which is why it must be loud at start rather than quiet at run time.
There is deliberately **no `UseNoMessaging()`** to opt out of it: unlike
`UseNoPersistence()`, which states a real intent ("this host commits nothing"), a host
that publishes nothing has nothing to declare — it just has no mapper. The remedy is to
configure the transport or delete the mapper.

A **domain event without a projection handler** is the opposite and stays unchecked, on
purpose. Several handlers per event are normal, no handler at all is normal, and a
context is free to project only the events its read models care about. The asymmetry is
not a gap: the mapper case has a single correct wiring and a silent failure, the
projection case has neither.

## Open questions

- Which Bounded Contexts (if any) justify Event Sourcing? _(To be decided per context.)_

## Related

- [Domain model](./domain-model.md)
- [Communication](./communication.md) (domain vs. integration events)
- [ADR-0019 — Event store technology (Marten on PostgreSQL)](./decisions/0019-event-store-technology-marten.md)
- [ADR-0020 — PostgreSQL for state-stored contexts; database per bounded context](./decisions/0020-postgresql-for-state-stored-contexts.md)
- [ADR-0021 — Write/read database pair per bounded context](./decisions/0021-write-read-database-pair-per-context.md)
- [ADR-0022 — Event-driven read models via an outbox-backed publisher](./decisions/0022-event-driven-read-models.md)
- [ADR-0023 — Wolverine as the messaging transport (replaces MassTransit)](./decisions/0023-wolverine-messaging-transport.md)
