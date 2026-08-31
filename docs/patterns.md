# Patterns

This document describes the design patterns every VitalSync service applies. The system-level view
is in [Architecture](./architecture.md); the products these patterns run on are in
[Technologies](./technologies.md).

## Domain-Driven Design

The domain layer is the heart of each service. It models the business explicitly and depends on
nothing else — no framework types, no infrastructure, no third-party libraries.

### Entity

An object with an identity that persists over time. Equality is based on identity: two entities are
equal when they are the same concrete type with the same id, regardless of their attribute values.

### Aggregate and Aggregate Root

An aggregate is a cluster of domain objects treated as one unit for data changes. Its root is the
consistency boundary and the only entry point: outside code addresses the root, never a child
directly.

An aggregate root exposes **behavior rather than setters**, so invariants cannot be bypassed, and
**raises domain events** to announce business-relevant changes. Its state is an immutable object
that the root evolves by applying events to it.

### Value Object

An immutable object defined entirely by its attributes, with structural equality. In VitalSync a
nutritional value, a quantity with a unit or a calorie amount are value objects.

### Strongly typed identifier

An aggregate identifier is a value object wrapping a primitive, not the primitive itself. A
`RecipeId` and an `IngredientId` are incompatible types even though both wrap a `Guid`, so passing
the wrong identifier is a compile-time error rather than a runtime bug.

### Domain event

A record of something business-relevant that happened, such as `RecipeCreated`. Domain events are
pure business data: no infrastructure types, no identity fields, plain value records with working
value equality. They are internal to one service and never reach the broker.

**The aggregate owns its domain events.** Only the aggregate may raise them; only the infrastructure
that dispatches them may clear them; everyone else gets a read-only view. This is enforced by the
type system rather than by convention.

```text
Aggregate raises event
        │
        ▼
Aggregate exposes events read-only
        │
        ▼
Persistence collects them on save ──► Outbox ──► Projections and integration events
        │
        ▼
Aggregate clears them, after the save succeeded
```

### Business rule versus domain validation

Two distinct concepts, deliberately not merged:

- A **business rule** is an invariant of the domain. Breaking it throws a business-rule violation.
- A **domain validation** is a constraint on input. Violating it throws a validation error.

Both are translated into a failed `Result` at the application boundary, so a caller sees a value
rather than an exception.

## CQRS

Every microservice separates write operations from read operations.

- **Commands** express intent and change state, for example *CreateRecipe* or
  *CompleteWorkoutSession*. A command returns a `Result`, or a `Result<T>` when a value is needed —
  by convention a create returns the new aggregate's typed id.
- **Queries** read state and never mutate it. A query returns a `Result<T>`.
- Each command and each query is handled by its own **dedicated handler**. Handlers are
  asynchronous and take a cancellation token.

```text
        write side                        read side
┌──────────────────────────┐    ┌──────────────────────────┐
│  Command ──► Handler     │    │  Query ──► Handler       │
│              │           │    │              │           │
│              ▼           │    │              ▼           │
│  Aggregate / Domain      │    │  Read model              │
└──────────────────────────┘    └──────────────────────────┘
```

Callers do not resolve handlers themselves; they send the command or query to a **dispatcher**,
which resolves the handler and runs the surrounding pipeline. **Pipeline behaviors** apply
cross-cutting concerns such as exception translation, logging and the unit of work, in an explicit
order.

### Result instead of exceptions

Expected failures are values, not exceptions. A `Result` is either success or one or more
**failures**, each carrying a stable machine-readable code, a human-readable message and a category
— validation, business rule, not found, or conflict. There is deliberately no "unexpected"
category: genuinely unexpected errors stay exceptions and bubble up to a thin global handler.

## Persistence

Two complementary strategies are used.

- **State-stored persistence is the default.** The aggregate's current state is persisted directly
  through an ORM.
- **Event Sourcing is selective.** The aggregate's state is derived from an append-only stream of
  domain events instead of being stored directly.

**Event Sourcing is applied only where it adds business value.** Which contexts justify it is still
open and is decided per context. Event Sourcing brings inherent history, audit and temporal
queries, at the cost of an event store, projections, event versioning and more operational surface.
Where that history has no business value, the cost buys nothing.

> **The choice is per bounded context, not per aggregate.** A microservice hosts exactly one bounded
> context, and a bounded context uses exactly one strategy — either all state-stored or all
> event-sourced, never both. The two stores live in separate databases, so a single commit cannot
> span them atomically. A context that appears to need both is a sign that it is **cut wrong** and
> should be split into two contexts, each in its own service with its own single strategy. The host
> configuration rejects a selection of both strategies at start-up.

Candidates worth evaluating for Event Sourcing are workout-session tracking, which forms a natural
event stream whose history has analytical value, and nutrient intake over time, which is
append-only by nature. Largely CRUD-shaped areas such as the ingredient catalog are better served by
state storage.

## The outbox and the read side

The read side of every context lives in its own read database and is kept current by an
outbox-backed publisher. The same mechanism is used for state-stored and event-sourced contexts
alike:

1. Handling a command makes the aggregate raise **domain events**.
2. Inside the **write transaction**, those events are also written to a **transactional outbox** in
   the write database, so they are captured atomically with the state change. No transaction spans
   two databases.
3. **After the commit**, the publisher drains the outbox and dispatches each event to the
   in-context **projection handlers**, which update the read database, and — where a mapper exists
   — to the **integration-event path** on the broker.
4. An outbox entry is marked processed only once its handlers have succeeded, which makes delivery
   **at-least-once**.

### Projections and read models

A read model is a representation shaped for a specific query. Read models are domain-shaped and
owned by the service, never mixed into the write tables, and always derived: a read database can be
dropped and reconstructed.

Because the write and read databases are separate, read-model updates are necessarily post-commit
and **eventually consistent**. Latency is typically low, but correctness must never depend on the
lag being zero. Read-your-writes is handled at the BFF or in the UI, where it matters.

Two obligations follow for every projection handler:

- **It must be idempotent.** At-least-once delivery means it will occasionally see the same event
  twice. Applying an event twice must not change the outcome.
- **It must be order-aware per aggregate.** Each read model records the aggregate version it last
  processed and ignores anything at or below it. Ordering across different aggregates is not
  guaranteed.

A read model is **rebuildable**, by different means depending on the strategy: an event-sourced
context replays its stream, while a state-stored context has no surviving history and instead
derives the read model again from the current aggregate state. This has a consequence worth stating
plainly: **every field of a state-stored read model must be a function of the current state.** A
rebuild writes absolute values, never increments. A field that needs history belongs in an
event-sourced context.

A rebuild is invoked explicitly, never automatically, and it publishes no integration events.

## Integration events

An integration event is a message published to the messaging backbone to communicate **across**
services. It is the counterpart of the domain event, which stays inside one service.

Domain events never reach the broker. Only contracts marked as integration events are published,
and a mapper at the service boundary translates a domain event into one where that is wanted.

### Event identity

Identity placement is deliberately asymmetric, and this is a decided rule rather than an
inconsistency:

- **Domain events carry no identity.** They are pure value records. Their identity and timestamp
  travel on the envelope, which is always present inside the owning service.
- **Integration events carry their identity on the event.** A foreign consumer knows about no
  envelope and needs a stable id on the contract itself.

A mapper takes both from the domain event's metadata. It must never mint a fresh identifier per
invocation — a redelivery would then arrive under a new identity, and deduplication would break.

### Consumer idempotency

Delivery is at-least-once, so a consumer must tolerate seeing the same integration event twice.
Never write a consumer that depends on being called exactly once.

Transport-level repetitions — a requeue, a crash before the acknowledgement, a broker reconnect, a
sender-side retry — are absorbed by a durable inbox that discards an envelope it has already
handled. That protection is time-boxed to a retention window of a few days, so it does not replace
an idempotent handler.

The sanctioned pattern for the business case is **shared identity**: a consumer that derives its own
aggregate from a foreign event adopts the foreign identity for it and treats an already existing
aggregate as success. Re-processing then overwrites one row instead of creating a second aggregate.

### Routing and topology

All integration events go to a single topic exchange. VitalSync defines its name once as
`VitalSyncMessaging.IntegrationEventExchangeName` in `VitalSync.ServiceDefaults`, and every host
uses that constant rather than a literal.

Each integration event has a mandatory, stable routing key of the form `<context>.<event>` in
kebab-case. The key is part of the published contract and is never derived from a namespace.

Consumers own their side of the topology: they choose their queue name and the topic patterns they
bind. Adding a subscriber never changes the publishing service.

**A service knows which context it is.** Each service is configured with one lower-case
bounded-context name, and that name is the first segment of every routing key it publishes. Three
rules follow from it:

- **Publishing under a foreign context is rejected.** The first segment names the owner of the
  contract, and consumers bind to it.
- **A context does not consume its own integration events.** A broad pattern in the owning service
  does not deliver its own events back to it.
- **A handler must be reachable.** Every integration event a service handles must be matched by at
  least one bound pattern outside its own context, otherwise the host refuses to start. This makes
  the previous rule lossless — it can never silently skip a handler.

Delivery is durable end to end: the exchange, the queues and the sending endpoint are all durable,
so an outgoing event is persisted before it is sent. Neither a broker restart nor a process crash
between commit and acknowledgement loses an integration event.
