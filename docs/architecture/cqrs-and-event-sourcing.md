# CQRS & Event Sourcing

## CQRS (mandatory)

Every microservice implements **Command Query Responsibility Segregation (CQRS)** to separate write operations from read operations.

- **Commands** change state and express intent (e.g., *CreateRecipe*, *CompleteWorkoutSession*). They return a `Result` (success/failure), or a `Result<T>` when a value is needed — e.g. a **create** command returns the new aggregate's strongly typed id (`Result<RecipeId>`) so the frontend can navigate to it. A **delete/void** command returns a plain `Result`.
- **Queries** read state and never mutate it; they return `Result<T>`.
- Commands and queries are handled by **dedicated handlers**.

```text
        write side                          read side
┌───────────────────────┐         ┌───────────────────────┐
│  Command ──► Handler   │         │  Query ──► Handler     │
│        │               │         │        │              │
│        ▼               │         │        ▼              │
│   Aggregate / Domain   │         │   Read model / store  │
└───────────────────────┘         └───────────────────────┘
```

The Application building block provides the `ICommand`, `IQuery`, and corresponding handler abstractions, a hand-rolled dispatcher, and the `Result` / `Error` model. Domain exceptions (`BusinessRuleViolationException`, `DomainValidationException`) are translated to `Result.Failure` at the Application boundary. See [Building Blocks](./building-blocks.md), the [BuildingBlocks.Application reference](./building-blocks-application.md), and [ADR-0015](./decisions/0015-hand-rolled-cqrs-mediator.md) / [ADR-0017](./decisions/0017-application-error-handling-and-result.md).

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

| Aspect | EF Core (state-stored) | Event Sourcing |
|---|---|---|
| Implementation complexity | Lower | Higher |
| Full audit/history | Not inherent | Inherent |
| Temporal queries / replay | Hard | Natural |
| Read models | Direct from tables | Usually via projections |
| Operational overhead | Lower | Higher (event store, projections, versioning) |

## Read models & projections

Regardless of write strategy, the read side may use **projections** optimized for queries. With Event Sourcing, projections are built by replaying events. With EF Core, read models can be the same tables or dedicated query models.

## Open questions

- Which Bounded Contexts (if any) justify Event Sourcing? *(To be decided.)*
- Event store technology, if Event Sourcing is adopted. *(To be decided.)*

## Related

- [Domain model](./domain-model.md)
- [Communication](./communication.md) (domain vs. integration events)