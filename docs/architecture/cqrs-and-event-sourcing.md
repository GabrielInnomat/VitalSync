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

The Application building block provides the `ICommand`, `IQuery`, and corresponding handler abstractions, a hand-rolled dispatcher, and the `Result` / `Failure` model. Domain exceptions (`BusinessRuleViolationException`, `DomainValidationException`) are translated to `Result.Failure` by an Application pipeline behavior.

## Persistence strategy

Two complementary approaches are used:

1. **Traditional persistence (default).** Most contexts use **Entity Framework Core** to persist aggregate state directly.
2. **Event Sourcing (selective).** Where it provides **business value**, an aggregate's state is derived from an append-only stream of domain events instead of being stored directly.

> **Decision rule:** Event Sourcing is applied **only where it adds business value**. In all other cases, EF Core is used. The exact contexts that justify Event Sourcing are **to be determined** during the project.

> **The choice is per bounded context, not per aggregate.** A microservice hosts exactly one bounded context, and a bounded context uses **exactly one** persistence strategy — either all state-stored (EF Core) or all event-sourced (Marten), never both. The two stores live in separate databases (see [ADR-0020](./decisions/0020-postgresql-for-state-stored-contexts.md)), so a single commit cannot span them atomically. A context that appears to need both an event-sourced and a state-stored aggregate is a sign it is **cut wrong** and should be split into two bounded contexts, each in its own microservice with its own single strategy. `AddBuildingBlocks` enforces this: selecting both `UseEfCorePersistence<TContext>(…)` and `UseMartenEventSourcing(...)` for the same host **throws at startup**.

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

## Event store technology

When a context is event-sourced, its events are persisted in **Marten on
PostgreSQL**, used as a **raw event store**: the event-sourced repository in
`BuildingBlocks.Infrastructure` tracks the aggregates it hands out, and the unit
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
3. After commit, the **Publisher** (in `BuildingBlocks.Infrastructure`) drains the
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
a Building Block; Infrastructure ships only the plumbing (Publisher, outbox,
dispatch loop, projection runner, transport). Read models are **rebuildable** by
replaying events (ES) or re-running projections over the write side.

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
