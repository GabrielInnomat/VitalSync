# CQRS & Event Sourcing

## CQRS (mandatory)

Every microservice implements **Command Query Responsibility Segregation (CQRS)** to separate write operations from read operations.

- **Commands** change state and express intent (e.g., _CreateRecipe_, _CompleteWorkoutSession_). They return a `Result` (success/failure), or a `Result<T>` when a value is needed — e.g. a **create** command returns the new aggregate's strongly typed id (`Result<RecipeId>`) so the frontend can navigate to it. A **delete/void** command returns a plain `Result`.
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

The Application building block provides the `ICommand`, `IQuery`, and corresponding handler abstractions, a hand-rolled dispatcher, and the `Result` / `Failure` model. Domain exceptions (`BusinessRuleViolationException`, `DomainValidationException`) are translated to `Result.Failure` at the Application boundary. See [Building Blocks](./building-blocks.md), the [BuildingBlocks.Application reference](./building-blocks-application.md), and [ADR-0015](./decisions/0015-hand-rolled-cqrs-mediator.md) / [ADR-0017](./decisions/0017-application-error-handling-and-result.md).

## Persistence strategy

Two complementary approaches are used:

1. **Traditional persistence (default).** Most contexts use **Entity Framework Core** to persist aggregate state directly.
2. **Event Sourcing (selective).** Where it provides **business value**, an aggregate's state is derived from an append-only stream of domain events instead of being stored directly.

> **Decision rule:** Event Sourcing is applied **only where it adds business value**. In all other cases, EF Core is used. The exact contexts that justify Event Sourcing are **to be determined** during the project.

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

**Each bounded context owns its own database.** Contexts never share a database,
and there are **no cross-database foreign keys, joins, or transactions** —
cross-context consistency is via integration events. Today all context databases
are hosted on **one shared PostgreSQL server** (in Aspire: one server resource with
one `AddDatabase(...)` per context). Moving a context onto its **own dedicated
server** later ("server per context") is an explicitly supported, non-breaking
migration: because each context already has its own database, `DbContext`,
migrations, and connection string, it is a connection-string change plus a data
move, touching no Domain/Application/Infrastructure code.

The event store and the state-stored store **never co-locate in the same
database**, even on the same server, so they can move and scale independently. See
[ADR-0020](./decisions/0020-postgresql-for-state-stored-contexts.md).

## Event store technology

When a context is event-sourced, its events are persisted in **Marten on
PostgreSQL**, used as a **raw event store**: the event-sourced repository in
`BuildingBlocks.Infrastructure` appends uncommitted domain events to the stream
(with optimistic concurrency asserted against the aggregate's `Version`) and, on
load, fetches the raw stream and folds it through the aggregate's own
`LoadFromHistory`. Marten's convention-based `Apply`-on-aggregate aggregation is
**not** used, so the domain (ADR-0010 / ADR-0012) is untouched.

**Snapshotting is deferred but non-breaking:** because a Marten snapshot is a
separate document and the event schema is identical with or without snapshots,
snapshotting can be added per context later with **no event migration**.

Marten is MIT-licensed and runs on PostgreSQL, which has a first-party .NET Aspire
hosting integration. See [ADR-0019](./decisions/0019-event-store-technology-marten.md).

## Read models & projections

Regardless of write strategy, the read side may use **projections** optimized for queries. With Event Sourcing, projections are built by replaying events. With EF Core, read models can be the same tables or dedicated query models.

## Open questions

- Which Bounded Contexts (if any) justify Event Sourcing? _(To be decided per context.)_

## Related

- [Domain model](./domain-model.md)
- [Communication](./communication.md) (domain vs. integration events)
- [ADR-0019 — Event store technology (Marten on PostgreSQL)](./decisions/0019-event-store-technology-marten.md)
- [ADR-0020 — PostgreSQL for state-stored contexts; database per bounded context](./decisions/0020-postgresql-for-state-stored-contexts.md)
