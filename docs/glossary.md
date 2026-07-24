# Glossary

A shared vocabulary for VitalSync. Terms will be added and refined as the domain evolves.

> Terms marked **(Building Block)** refer to concrete types or concepts in `BuildingBlocks.Domain`. See the [BuildingBlocks.Domain technical reference](../BuildingBlocks/docs/architecture/building-blocks-domain.md) for details. Terms marked **(Application)** refer to concepts in `BuildingBlocks.Application`; see the [BuildingBlocks.Application reference](./architecture/building-blocks-application.md).

## Architecture & patterns

| Term                           | Definition                                                                                                                                                                                              |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Aggregate**                  | A cluster of domain objects treated as a single unit for data changes, with a consistency boundary.                                                                                                    |
| **Aggregate Root**             | The entry point to an aggregate; enforces invariants and owns its domain events. In VitalSync, modeled by the single base `AggregateRoot<TKey, TState>` **(Building Block)**, which serves both event-sourced and state-stored persistence. |
| **State Object**               | **(Building Block)** A per-aggregate object (`IState<TKey>`) that holds the aggregate's current state, **owns its identity**, and contains all event-application ("apply"/"evolve") logic. Keeps the aggregate class free of per-event mutation code. State objects are immutable: `Apply` returns the next state. |
| **Apply (Evolve)**             | The function that produces the next state from the current state and a domain event (`IState<TKey>.Apply`). Used both when raising new events and when replaying history. |
| **Rehydration**                | Rebuilding an aggregate's state by replaying its persisted event stream (`LoadFromHistory`). Used by event-sourced aggregates. |
| **Version**                    | **(Building Block)** An aggregate's stream position — the number of events applied to it. Meaningful for event-sourced aggregates (concurrency / stream length); unused for state-stored aggregates. |
| **BFF (Backend-for-Frontend)** | A backend tailored to the needs of a specific frontend; the only component the Blazor client talks to. |
| **Bounded Context**            | An explicit boundary within which a domain model is defined and applicable. |
| **CQRS**                       | Command Query Responsibility Segregation: separating write operations (commands) from read operations (queries). |
| **Code-first gRPC**            | Defining gRPC service contracts in C# code rather than hand-authored `.proto` files. |
| **Domain Event**               | **(Building Block)** A record of something business-relevant that happened inside a domain (`IDomainEvent`); pure business data carrying a stable `EventId` and an `OccurredAt` timestamp. Internal to a service's domain. |
| **Integration Event**          | A message published across service boundaries to communicate something to other services. Distinct from a domain event; produced by translating domain events at the service boundary. |
| **Event Sourcing (ES)**        | Persisting an aggregate as an append-only stream of domain events, rebuilding current state by replaying them through the state object's `Apply`. Used **where it provides business value**; in all other cases EF Core state-stored persistence is used. |
| **State-stored persistence**   | Persisting an aggregate's current `State` directly (via EF Core), rather than as a stream of events. The default where Event Sourcing is not warranted. |
| **Entity**                     | **(Building Block)** A domain object with a distinct identity that runs through time (`Entity<TKey>`). Identity is constructor-set and get-only; equality is identity-based. |
| **Strongly Typed Identifier**  | **(Building Block)** A value wrapping a primitive id (`IEntityKey`, a `readonly record struct` backed by a `Guid`) so identifiers of different aggregates are distinct types and cannot be interchanged at compile time. |
| **Outbox**                     | A pattern where outgoing messages/events are stored with the local transaction and published reliably afterward. Relies on each event's `EventId` for idempotency/deduplication on the consumer side. |
| **Projection**                 | A read model built (often by replaying events) for efficient querying. |
| **Value Object**               | An immutable domain object defined by its attributes, with structural equality. |

## Domain-event ownership & lifecycle

| Term                                   | Definition                                                                                                                                               |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Domain Event Ownership**             | The rule that an aggregate is the **sole owner** of its domain events: only the aggregate may raise them, only privileged infrastructure may clear them, and all other layers get a read-only view. |
| **Read-only domain events**            | **(Building Block)** Access to an aggregate's events through `IHasDomainEvents.DomainEvents` — a read-only collection available to any layer. Reading is side-effect free (it does not clear). |
| **Domain Events Manager**              | **(Building Block)** The privileged contract (`IDomainEventsManager`) that can **clear** an aggregate's events. Implemented **explicitly** so clearing is not on the aggregate's public surface; only the persistence layer (after a successful save) casts to it. |
| **Raise (an event)**                   | The aggregate-internal act of applying a new domain event to its state and recording it (`RaiseEvent`). `protected` — only the aggregate itself can raise. |
| **Per-transition identity validation** | The rule that an aggregate's identity (`State.Id`) is checked for validity (non-empty GUID) immediately **after each event is applied**, in both `RaiseEvent` and `LoadFromHistory`. The first event must set the identity; no later event may blank it. There is no separate post-creation check. |

## Rules & validation

| Term                          | Definition                                                                                                                                                                                                |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Business Rule (Invariant)** | **(Building Block)** A condition that must always hold for the domain to be valid (`IBusinessRule`, predicate `IsBroken()`). Violations throw `BusinessRuleViolationException`. |
| **Domain Validation Rule**    | **(Building Block)** A constraint on incoming values (`IDomainValidationRule`, predicate `IsInvalid()`). Failures throw `DomainValidationException`. |
| **Rule Checker**              | **(Building Block)** A helper (`RuleChecker`) that evaluates business rules and domain validation rules, throwing the matching exception; the `params` overload short-circuits on the first failure. |
| **Clock**                     | **(Building Block)** An abstraction over "now" (`IClock`) used to source domain-event timestamps deterministically and keep time-dependent logic testable. |

## Application & CQRS

| Term                     | Definition                                                                                                                                                                                                |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Command**              | **(Application)** An intent to change state (`ICommand` / `ICommand<TResult>`). Returns a `Result` (success/failure) or a `Result<TResult>` when a value is needed — e.g. a create command returns the new aggregate's strongly typed id. |
| **Query**                | **(Application)** A read-only request (`IQuery<TResult>`) that never mutates state and returns `Result<TResult>`. |
| **Command Handler**      | **(Application)** The single dedicated handler for a command (`ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResult>`); async-only with a `CancellationToken`. |
| **Query Handler**        | **(Application)** The single dedicated handler for a query (`IQueryHandler<TQuery, TResult>`); async-only with a `CancellationToken`. |
| **Pipeline Behavior**    | **(Application)** A cross-cutting wrapper around handler execution (`IPipelineBehavior<TRequest, TResponse>`) for concerns like exception-to-`Result` translation, logging, or unit-of-work. Behaviors run in explicit DI registration order. |
| **Dispatcher (Sender)**  | **(Application)** The single entry point (`ISender`) that routes a command/query to its handler through the pipeline. The contract lives in `Application`; the DI-based implementation lives in `Infrastructure` (a hand-rolled mediator, not MediatR). |
| **Result**               | **(Application)** The uniform outcome of a command/query: `Result` (success or failure) or `Result<T>` (success carrying a value). Failure carries one or more `Error`s. |
| **Error**                | **(Application)** A structured failure with a stable machine-readable `Code`, a human-readable `Message`, and an `ErrorCategory`. |
| **ErrorCategory**        | **(Application)** The semantic category of a failure — one of `Validation`, `BusinessRule`, `NotFound`, `Conflict`. Drives transport status mapping at the BFF/service host; the Application layer never references HTTP/gRPC. |

## Building Blocks platform

| Term                          | Definition                                                                                                                                                            |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Building Blocks**           | A reusable platform of shared concepts and components (Domain, Application, Infrastructure, Persistence, Event Processing) that underpins the microservices without coupling them to VitalSync, and is reusable in future projects. |
| **BuildingBlocks.Domain**     | The foundational, infrastructure-free building block providing the tactical DDD primitives (entities, the unified aggregate, state objects, domain events, strongly typed ids, rules, exceptions). Declares no package dependencies. |
| **BuildingBlocks.Application** | The framework-agnostic building block providing the CQRS abstractions (commands, queries, handlers), the pipeline behavior and dispatcher contracts, and the `Result` / `Error` model. Depends only on `Domain`. |

## Business domains

| Term                   | Definition                                                         |
| ---------------------- | ------------------------------------------------------------------ |
| **Ingredient**         | A food item with nutritional values, used in recipes.              |
| **Recipe**             | A composition of ingredients with preparation information.         |
| **Meal Plan**          | A planned arrangement of meals/recipes over time.                  |
| **Shopping List**      | A generated list of items needed, derived from recipes/meal plans. |
| **Nutrient Intake**    | Calculated nutrients consumed, based on logged meals.              |
| **Exercise**           | A physical activity that can be part of a workout.                 |
| **Workout Plan**       | A planned arrangement of exercises.                                |
| **Workout Session**    | A tracked, completed workout occurrence.                           |
| **Energy Expenditure** | Calories burned, determined from workout activity.                 |

> Terms in the business domains section are provisional and will be refined as Bounded Contexts are clarified.
