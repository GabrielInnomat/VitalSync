# Glossary

A reference for the vocabulary used across VitalSync — both the **business
concepts** (what the product does) and the **technical concepts, patterns, and
architecture** (how it is built). It is written to be worth reading for
experienced engineers who want to understand VitalSync's use cases or the
design decisions behind it.

## How this glossary is organized

Terms are grouped by theme. **Within each theme they are ordered alphabetically**
for predictable lookup; where one term builds on another the definition links to
it directly. Cross-references point to the deeper documents under
[`docs/architecture`](./architecture/overview.md) and the
[Architecture Decision Records](./architecture/decisions/README.md) (ADRs).

- [Product & business domains](#product--business-domains)
- [Bounded contexts & the ubiquitous language](#bounded-contexts--the-ubiquitous-language)
- [Tactical Domain-Driven Design](#tactical-domain-driven-design)
- [CQRS & the application layer](#cqrs--the-application-layer)
- [Event Sourcing & persistence](#event-sourcing--persistence)
- [Messaging & communication](#messaging--communication)
- [The Building Blocks platform](#the-building-blocks-platform)
- [Technology & operations](#technology--operations)
- [Testing](#testing)
- [Process & conventions](#process--conventions)

---

## Product & business domains

### Analytics & Reporting

The business domain that derives insights and reports from the data produced by
Nutrition and Fitness. Its concrete requirements are intentionally identified and
extended as the project evolves.

### Fitness

The business domain concerned with physical activity. Its use cases include
managing **exercises**, creating **workout plans**, tracking completed **workout
sessions**, and determining energy expenditure / calories burned.

### Ingredient

A Nutrition concept: a single food item together with its nutritional values. The
ingredient catalog is largely CRUD-shaped and is therefore a candidate for
traditional (EF Core) persistence rather than Event Sourcing.

### Meal plan

A Nutrition concept that composes recipes/meals over time; the basis for shopping
lists and for calculating nutrient intake.

### Nutrition

The business domain concerned with food and diet. Its use cases include managing
**ingredients** and their nutritional values, creating **recipes**, composing
**meal plans**, generating shopping lists, and calculating **nutrient intake**
from consumed meals. It is the most developed domain in the current
[user stories](./userStories/nutrition/001_createRecipe.md).

### Recipe

A Nutrition **aggregate** composed of ingredients and quantities. It is the
canonical example used throughout the architecture docs (e.g. `Recipe`,
`RecipeId`, `RecipeCreated`) to illustrate aggregates, strongly typed
identifiers, and domain events.

### VitalSync

The product: a **cloud-native, distributed platform** that unifies **nutrition**,
**fitness**, and **health analytics** behind a single user experience. Users
manage diet and workout data and derive insights from it. A guiding principle of
the project is *"the architecture is fixed, the domain is fluid"* — technical
decisions are mandatory and stable, while business requirements are refined
iteratively.

### Workout session

A Fitness concept representing one completed training session (started, exercises
logged, completed). Because it forms a natural event stream and its full history
has analytical value, it is a **candidate** for Event Sourcing (still to be
decided — see [CQRS & Event Sourcing](./architecture/cqrs-and-event-sourcing.md)).

---

## Bounded contexts & the ubiquitous language

### Bounded Context

A boundary within which a domain model and its **ubiquitous language** are
consistent and self-contained. VitalSync's candidate contexts are **Nutrition**,
**Fitness**, and **Analytics**; the final decomposition is deliberately part of
the project and refined iteratively. Contexts never share a domain model.

### Domain-Driven Design (DDD)

The overarching design approach. VitalSync applies DDD in every microservice:
the **domain layer is the heart** of each service, models the business explicitly,
and is kept independent of infrastructure. See
[Domain model](./architecture/domain-model.md).

### Microservice

An independently deployable service that owns one business area and its data.
Services do **not** share databases and avoid synchronous coupling, which
preserves independent deployability. VitalSync has one service per business area
(Nutrition, Fitness, Analytics).

### Ubiquitous Language

The shared, precise vocabulary used consistently by domain experts and code
within a bounded context. This glossary is, in part, an entry point into that
language.

---

## Tactical Domain-Driven Design

These are the building-block primitives provided by
[`BuildingBlocks.Domain`](./architecture/building-blocks-domain.md) and used by
every service's domain layer.

### Aggregate

A cluster of domain objects treated as a single unit for data changes, bounded by
its **Aggregate Root**.

### Aggregate owns its domain events

A core ownership rule: **only** the aggregate may raise events; **only** a
privileged infrastructure contract may clear them; everyone else gets a
**read-only** view. This is enforced structurally via two interfaces —
`IHasDomainEvents` (read-only, everyone) and `IDomainEventOwner` (clear,
infrastructure-only, implemented explicitly; named `IDomainEventsManager` before
the ADR-0007 naming amendment). See
[ADR-0006](./architecture/decisions/0006-aggregate-owns-domain-events.md) and
[ADR-0007](./architecture/decisions/0007-read-only-vs-managed-domain-events.md).

### Aggregate Root

The consistency boundary and single entry point for an aggregate. It exposes
**behavior (not setters)** to enforce invariants, **raises domain events** to
announce business-relevant changes, and exposes those events **read-only** to
other layers. VitalSync provides a single authoring model with two bases:

- `AggregateRoot<TKey, TState>` — the base for **every** aggregate; identity is
  derived from state; state changes only via `RaiseEvent`, which folds the
  event into the immutable state.
- `EventSourcedAggregateRoot<TKey, TState>` — additive base for
  **event-sourced** aggregates; adds only `Version` and `LoadFromHistory`.

The author picks the event-sourced base only when the event history itself
carries business value; the persistence style is chosen in the composition
layer ([ADR-0025](./architecture/decisions/0025-unified-state-fold-aggregate-model.md)).

### Business Rule vs. Domain Validation

Two distinct concepts, each with its own rule interface and exception:

- **Business rule** (invariant) — `IBusinessRule.IsBroken()`, throws
  `BusinessRuleViolationException`.
- **Domain validation** (input constraint) — `IDomainValidationRule.IsInvalid()`,
  throws `DomainValidationException`.

`RuleChecker` evaluates them and throws the matching exception. See
[ADR-0009](./architecture/decisions/0009-business-rules-and-domain-validation.md).

### Domain Event

A record of something **business-relevant** that has happened in the domain (e.g.
`RecipeCreated`, `RecipeRenamed`). Domain events are **pure business data** —
no infrastructure or third-party types, no identity fields — plain value records
with working value equality, **internal** to a service. Their `EventId` and
`OccurredAt` are minted at commit and travel on the `DomainEventEnvelope`
(ADR-0029). They may be translated into an **integration event** at the service
boundary, which carries that identity on the event itself.

### Entity

An object with an **identity** that persists over time. Equality is based on
identity — two entities are equal when they are the **same concrete type** and
have the **same id** (not on their attribute values). Every entity has a
[state](#entity-state-entitystatetself-tkey): the two bases are
`Entity<TKey, TState>` for a [child entity](#child-entity-entitytkey-tstate) and
`AggregateRoot<TKey, TState>` for an [aggregate root](#aggregate-root), and both
derive from `EntityBase<TKey>`, which holds the one equality implementation. See
[ADR-0008](./architecture/decisions/0008-entity-identity-and-equality.md) and
[ADR-0032](./architecture/decisions/0032-child-entities-raise-via-root.md).

### Child Entity (`Entity<TKey, TState>`)

An [entity](#entity) that lives **inside** an aggregate and carries its own
invariants. Its data is an [entity state](#entity-state-entitystatetself-tkey)
held in the root's state; the class itself is a thin hull the root builds on
demand, which reads its state **through the root** (`GetCurrentState()`, a method
because it throws once the child is gone) and raises events through the root's
`IDomainEventRaiser` channel. It has **no** event list and **no** version of its
own — one list and one version, both on the root. See
[ADR-0032](./architecture/decisions/0032-child-entities-raise-via-root.md).

### Entity State (`EntityState<TSelf, TKey>`)

The child counterpart of the [state object](#state-object-aggregatestatetself-tkey):
an immutable record with an identity and a pure `Apply`, but **without** a
version. The root's state folds its children, usually by delegating to their
`Apply`. Persisted as an **owned type**
([ADR-0031](./architecture/decisions/0031-aggregate-child-collections-as-owned-types.md)).

### State object (`AggregateState<TSelf, TKey>`)

An **immutable** record that owns the aggregate's identity, its **version**, and
all **apply / evolve** logic. `Apply(IDomainEvent)` returns the next state
(`this with { … }`). Keeping evolution logic on the state keeps large aggregates
free of "apply noise" — the aggregate class holds only the public command API.
It is an abstract record rather than an interface so the base can own the version
bookkeeping via the virtual record copy constructor. See
[ADR-0010](./architecture/decisions/0010-aggregate-state-object.md) and
[ADR-0030](./architecture/decisions/0030-persisted-names-and-aggregate-version.md).

### Strongly Typed Identifier

An aggregate identifier modeled as a **Value Object** (a `readonly record struct`
implementing `IEntityKey<TValue>`) rather than a raw primitive. A `RecipeId` and
an `IngredientId` are distinct, incompatible types even though both wrap a `Guid`,
so passing the wrong id is a **compile-time error**. Each key defines its own
`IsEmpty` rule, making identity validation type-agnostic. See
[ADR-0005](./architecture/decisions/0005-strongly-typed-aggregate-identifiers.md).

### Value Object

An immutable object defined entirely by its attributes, with **structural
equality**. Examples in VitalSync: a nutritional value, a quantity with a unit, a
calorie amount.

---

## CQRS & the application layer

### Command

An object that expresses **intent** and **changes state** (e.g. `CreateRecipe`,
`CompleteWorkoutSession`). Marked with `ICommand` (returns `Task<Result>`) or
`ICommand<TResult>` (returns `Task<Result<TResult>>`). By convention, **create**
commands return the new aggregate's strongly typed id; **delete/update** commands
return a plain `Result`.

### CQRS (Command Query Responsibility Segregation)

A **mandatory** pattern in every microservice: write operations (**commands**) are
separated from read operations (**queries**), each handled by a dedicated handler.
The abstractions live in
[`BuildingBlocks.Application`](./architecture/building-blocks-application.md).

### Dispatcher (`ISender`) / hand-rolled mediator

The single entry point callers use to send a command or query. VitalSync uses a
**hand-rolled** mediator instead of MediatR: the `ISender` contract lives in
`Application`, its DI-based implementation in `Infrastructure` resolves the
matching handler and pipeline. The dispatcher is deliberately **not** replaced by
Wolverine even though Wolverine is now the messaging transport
([ADR-0023](./architecture/decisions/0023-wolverine-messaging-transport.md)) —
Wolverine stays transport-only to keep the framework-agnostic core decoupled. See
[ADR-0015](./architecture/decisions/0015-hand-rolled-cqrs-mediator.md).

### Exception-to-Result translation

The rule that expected **domain exceptions** (`BusinessRuleViolationException`,
`DomainValidationException`) are translated into `Result.Failure` by an
`ExceptionToResultBehavior` at the application boundary, while unexpected
exceptions bubble up. See
[ADR-0017](./architecture/decisions/0017-application-error-handling-and-result.md).

### Failure / FailureCategory

A structured error: a stable machine-readable `Code` (e.g. `recipe.name_required`),
a human-readable `Message`, and a `Category`. Categories are `Validation`,
`BusinessRule`, `NotFound`, and `Conflict`. There is deliberately **no**
`Unexpected` category — unexpected errors stay exceptions and bubble to a thin
global handler.

### Handler

The single dedicated class that processes one command or query
(`ICommandHandler<…>`, `IQueryHandler<…>`). Handlers are **async-only** —
they return a `Task<…>` and accept a `CancellationToken`.

### Pipeline behavior

A wrapper around handler execution that applies **cross-cutting concerns**
(exception translation, logging, unit-of-work, validation). Behaviors run in an
**explicit numeric order** (lower orders wrap further out); the built-ins occupy
fixed slots and hosts add their own via `AddPipelineBehavior(type, order)`. Only
the `IPipelineBehavior<TRequest, TResponse>` contract lives in `Application`, the
concrete behaviors live in `Infrastructure`.

### Query

An object that **reads state and never mutates it**. Marked with `IQuery<TResult>`
and handled by an `IQueryHandler<TQuery, TResult>` returning `Task<Result<TResult>>`.

### `Result` / `Result<T>`

The uniform outcome model returned by handlers. `Result` is success or a failure
carrying one or more `Failure`s; `Result<T>` additionally carries a value on
success. It lives in `BuildingBlocks.Application`. See
[ADR-0016](./architecture/decisions/0016-remove-common-result-in-application.md).

---

## Event Sourcing & persistence

### Database per bounded context

The database topology: **each bounded context owns its own databases**, never
shared, with **no cross-database foreign keys, joins, or transactions**
(cross-context consistency is via integration events). The unit of ownership is a
**[write+read pair](#write-database--read-database-writeread-pair)** — each context
owns exactly two PostgreSQL databases. Today all context databases are hosted on
**one shared PostgreSQL server** (in Aspire: one server resource with **two
`AddDatabase(...)` calls per context**, e.g. `nutrition-write` and
`nutrition-read`). Moving either database of a context onto its **own dedicated
server** later is a sanctioned, non-breaking migration — a connection-string change
plus a data move, touching no application code. See
[ADR-0020](./architecture/decisions/0020-postgresql-for-state-stored-contexts.md)
and
[ADR-0021](./architecture/decisions/0021-write-read-database-pair-per-context.md).

### Event Sourcing (ES)

A persistence approach in which an aggregate's state is derived from an
**append-only stream of domain events** rather than being stored directly. It
provides inherent audit/history and natural temporal queries / replay, at the
cost of higher complexity (event store, projections, versioning). Backed by the
[event store](#event-store-marten-on-postgresql).

### Event store (Marten on PostgreSQL)

The technology backing event-sourced contexts: [Marten](#marten) on
[PostgreSQL](#postgresql), used as a **raw event store**. The event-sourced
repository appends uncommitted domain events to the stream (with optimistic
concurrency on [`Version`](#version-stream-position)) and, on load, fetches the raw
stream and folds it through the aggregate's own
[`LoadFromHistory`](#raiseevent--loadfromhistory) — Marten's convention-based
`Apply`-on-aggregate aggregation is **not** used, keeping the domain untouched.
[Snapshotting](#snapshotting) is deferred but can be added per context without an
event-schema migration. The Marten streams (`mt_events` / `mt_streams`) live in the
context's **write database**
([ADR-0021](./architecture/decisions/0021-write-read-database-pair-per-context.md)).
See
[ADR-0019](./architecture/decisions/0019-event-store-technology-marten.md).

### Eventual consistency (read side)

Because a context's write and read databases are **separate** PostgreSQL databases,
no single local transaction spans both, so read-model updates are **necessarily
post-commit and eventually consistent** — typically low-latency (the
[Publisher](#publisher-outbox-backed) drains immediately after commit), but
correctness never depends on the lag being zero. **Read-your-writes** is handled at
the BFF/UI where it matters. See
[ADR-0022](./architecture/decisions/0022-event-driven-read-models.md).

### Persistence strategy (selective Event Sourcing)

VitalSync uses **two complementary approaches**. Traditional
[state-storage](#state-stored-traditional-persistence) (**EF Core**) is the default;
[Event Sourcing](#event-sourcing-es) is applied *only where it adds business value*.
The exact contexts that justify Event Sourcing are still to be decided. See
[CQRS & Event Sourcing](./architecture/cqrs-and-event-sourcing.md).

### Projection / Read model

A representation of data **optimized for queries** on the read side of CQRS. Read
models live in a context's dedicated **[read database](#write-database--read-database-writeread-pair)**
— never mixed into the write tables — and are updated **uniformly** for both
event-sourced and state-stored contexts by replaying/handling the context's
**domain events** after the write commits (via a
[projection handler](#projection-handler)). They are **derived and rebuildable**: a
read database can be dropped and reconstructed by replaying events (ES) or
re-running projections over the write side. See
[ADR-0021](./architecture/decisions/0021-write-read-database-pair-per-context.md)
and
[ADR-0022](./architecture/decisions/0022-event-driven-read-models.md).

### Projection handler

An **in-context** handler that applies a domain event to a
[read model](#projection--read-model) in the read database. Because delivery is
at-least-once, projection handlers **must** be **idempotent** (applying an event
twice yields the same state — e.g. upsert by key) and **order-aware** (each read
model tracks a **last-processed position/version** per aggregate/stream and skips
events at or below it). Cross-aggregate ordering is **not** guaranteed. In-context
projections consume **domain** events directly; cross-context read data arrives only
via **integration** events. Read models are **not** a Building Block — they are
domain-shaped and owned by each service. See
[ADR-0022](./architecture/decisions/0022-event-driven-read-models.md).

### Publisher (outbox-backed)

The `BuildingBlocks.Infrastructure` component that, **after** the write commits,
**drains the transactional [outbox](#outbox-transactional-outbox)** and dispatches
each domain event to (a) **in-context [projection handlers](#projection-handler)**
that update the read database and (b) the **integration-event path** to
RabbitMQ/Wolverine. Delivery is **at-least-once**: an outbox entry is marked
processed only after its handlers succeed, otherwise it is retried. On the
integration-event path the RabbitMQ sending endpoint is **durable**, so the event is
persisted as an outgoing envelope before it is sent and arrives at the broker as a
**persistent** message on a **durable, quorum** topology. See
[ADR-0022](./architecture/decisions/0022-event-driven-read-models.md) and the
persistent-delivery amendment to
[ADR-0023](./architecture/decisions/0023-wolverine-messaging-transport.md).

### RaiseEvent / LoadFromHistory

The two ways an event-sourced aggregate's state changes: `RaiseEvent(e)`
applies the event to the state, validates identity, advances the
version, and records it; `LoadFromHistory(history)` **replays** a persisted stream
to rebuild state ([rehydration](#rehydration), recording nothing) into the hull
supplied by [reconstitution](#reconstitution). A
**replay-misuse guard** prevents `LoadFromHistory` from running after uncommitted
events exist. A [child entity](#child-entity-entitytkey-tstate) reaches the same
`RaiseEvent` through the root's channel, so both paths behave identically for
child-raised events.

### Reconstitution

Rebuilding an aggregate that **already exists**, as opposed to creating a new one.
A repository does not author aggregates — it restores a persisted state or replays
an event history — and both need an instance to fold into first. That instance
comes from the aggregate's **private parameterless constructor**, invoked through
an internal, per-type-cached factory in `Infrastructure`. The private constructor
keeps `new Widget()` a compile error, so the aggregate's own named factory stays
the only public way into existence, and the domain never sees an unidentified
hull. The convention is validated at **host startup**: `AddBuildingBlocks` scans
the `AddDomainEventsFrom` assemblies and fails registration, naming the aggregate,
if the constructor is missing. An earlier design expressed this as an explicit
`IReconstitutable<TSelf>` implementation (`static abstract CreateEmpty`) on every
aggregate; it was retired because the per-aggregate ceremony outweighed the
compile-time proof — see the reconstitution amendments of
[ADR-0025](./architecture/decisions/0025-unified-state-fold-aggregate-model.md).

### Rehydration

Rebuilding an aggregate's current state inside its repository: replaying the event
stream via [`LoadFromHistory`](#raiseevent--loadfromhistory) for an event-sourced
aggregate, or restoring the persisted state via `IStateOwner.Restore` for a
state-stored one. Both start from the empty hull supplied by
[reconstitution](#reconstitution), and both bring the aggregate's
[child entities](#child-entity-entitytkey-tstate) back with it, because the
children are part of the state that is replayed or restored.

### Snapshotting

An optional optimization for event-sourced aggregates: persist the aggregate's
`State` at a known version so a load can start from the snapshot and replay only
the tail of the stream instead of the whole history. In VitalSync it is
**deferred**; because a Marten snapshot is a separate document and the event schema
is unchanged, it can be introduced later per context with **no event migration**.

### State-stored (traditional) persistence

The default approach: the aggregate object is persisted directly via
[EF Core](#entity-framework-core-ef-core). Simpler than
[Event Sourcing](#event-sourcing-es), but without inherent history or replay.

### Unit of Work

The infrastructure component that groups changes into a single transactional
save. On save, it collects the aggregates' domain events, hands them to the
dispatcher/outbox, and clears them **only after** the save succeeds. Integration
events are enqueued to the [Wolverine](#wolverine) outbox **within this same
transaction**, so they commit atomically with the state change and are delivered
after commit ([ADR-0023](./architecture/decisions/0023-wolverine-messaging-transport.md)).

### Version (stream position)

The monotonic position of an event-sourced aggregate within its stream, advanced
on every `RaiseEvent`. Used for ordering and optimistic **concurrency** (asserted
as the expected revision when appending to the Marten event stream).

### Write database / Read database (write+read pair)

The two databases every bounded context owns. The **write database** holds the
authoritative state (EF Core tables for state-stored contexts; Marten event streams
for event-sourced contexts). The **read database** holds **query-optimized
[read models](#projection--read-model)**, updated from domain events after the write
commits, and is **derived and rebuildable** (never the system of record). Both
belong to one context and are never shared. See
[ADR-0021](./architecture/decisions/0021-write-read-database-pair-per-context.md).

---

## Messaging & communication

### Asynchronous messaging

The only channel for inter-service communication, chosen to maximize loose
coupling and independent deployability and to prevent distributed call chains and
temporal coupling. See
[ADR-0023](./architecture/decisions/0023-wolverine-messaging-transport.md) (which
supersedes [ADR-0004](./architecture/decisions/0004-asynchronous-messaging-between-services.md)).

### Backend-for-Frontend (BFF)

The single entry point for the frontend. It exposes **REST** to the Blazor client
and orchestrates calls to microservices via **code-first gRPC**. It centralizes
cross-cutting concerns (auth, aggregation, response shaping) and decouples the UI
from the service topology. See
[ADR-0003](./architecture/decisions/0003-bff-with-rest-and-code-first-grpc.md).

### Code-first gRPC

The synchronous, strongly typed RPC used between the BFF and microservices, with
contracts defined **in C#** rather than hand-authored `.proto` files.

### Communication rules

The mandatory topology: **Frontend → BFF** (REST) → **Microservice** (gRPC), and
**Microservice ↔ Microservice** asynchronous **only**. There is no direct
synchronous service-to-service communication. See
[Communication](./architecture/communication.md).

### Frontend (Blazor)

The user interface. It holds **no business logic** and communicates
**exclusively** through the BFF — never directly with a microservice.

### Integration Event

A message published to the messaging backbone to communicate **across services**.
Distinct from a **domain event** (internal to one service); domain events are
translated into integration events at the service boundary.

Its routing key is declared on the contract as
`[IntegrationEventTopic("<context>.<event>")]`, and the first segment names the
**owning bounded context**. A service declares its own context name when it
configures messaging, which makes three things enforceable: it cannot publish
under a foreign context, it never consumes an event it published itself (every
event carries a `buildingblocks.source-context` header, and a consumer-side
middleware drops its own), and a handler whose topic no bound pattern matches
fails the host at start-up (ADR-0023 amendment 2026-08-05).

### Platform exchange

The single RabbitMQ topic exchange all integration events are published to. Its
name is **not** a Building Blocks constant — the host supplies it, so the package
stays product-independent (ADR-0018 amendment 2026-08-05). VitalSync defines it
once as `VitalSyncMessaging.IntegrationEventExchangeName`
(`vitalsync.integration-events`) in `VitalSync.ServiceDefaults`; every host passes
that constant rather than a literal.

### Outbox (transactional outbox)

A reliability pattern: domain events collected on save are written and then
forwarded after commit, ensuring messages are not lost even if the process fails
after committing state. In VitalSync the **same** outbox is written in the write
transaction and drained after commit by the [Publisher](#publisher-outbox-backed)
to drive **two** paths: the **integration-event** path to RabbitMQ via
[Wolverine](#wolverine) (ADR-0023) **and** the **in-context read-model projections**
in the read database. This gives **at-least-once** delivery and closes the
crash-window drift a naive in-memory publisher would have across two databases. See
[ADR-0022](./architecture/decisions/0022-event-driven-read-models.md).

### RabbitMQ

The chosen messaging platform (the message broker) for asynchronous inter-service
communication.

### Wolverine

The **MIT-licensed** abstraction over RabbitMQ, providing publish/subscribe, the
**transactional outbox**, retries, and dead-lettering. It replaced **MassTransit**
(which moved to a commercial license) and runs **side-by-side with**
[Marten](#marten), enqueuing integration events to its outbox **inside the write
transaction** and delivering them after commit. The transport is configured for
**durable delivery**: durable exchange, quorum queues, and a durable sending
endpoint, so an integration event survives both a broker restart and a process
crash. Wolverine is used **only** as the
messaging transport — **not** as the in-process CQRS
[dispatcher](#dispatcher-isender--hand-rolled-mediator), which stays hand-rolled
(ADR-0015). See
[ADR-0023](./architecture/decisions/0023-wolverine-messaging-transport.md).

---

## The Building Blocks platform

### Building Blocks

A **reusable platform of shared primitives** that underpins the microservices
**without coupling them to VitalSync**, so it can be reused in future projects.
Split into exactly **three** packages by a **purity / dependency** boundary, not a
functional one. See [Building Blocks](./architecture/building-blocks.md) and
[ADR-0018](./architecture/decisions/0018-three-building-block-packages.md).

### BuildingBlocks.Application

The **framework-agnostic use-case layer**: CQRS contracts, the pipeline-behavior
and dispatcher contracts, and the `Result` / `Failure` model. Depends **only** on
`Domain` and holds **contracts, not implementations**. See
[reference](./architecture/building-blocks-application.md).

### BuildingBlocks.Domain

The **pure core**: tactical DDD primitives (entities, the two aggregate bases,
strongly typed keys, domain events, business-rule/validation abstractions). It has
**zero** third-party dependencies — BCL only. See
[reference](./architecture/building-blocks-domain.md).

### BuildingBlocks.Infrastructure

The **single outer layer** holding all reusable, framework-bound,
third-party-backed implementations that are still VitalSync-agnostic: unit of work,
generic repositories (EF Core and the Marten-based event store, see
[ADR-0019](./architecture/decisions/0019-event-store-technology-marten.md)), domain-
and integration-event dispatching, the RabbitMQ/Wolverine transport, and the
DI-based CQRS dispatcher and pipeline behaviors. Depends on both `Domain` and
`Application`.

### Purity boundary

The rule that decides where code belongs: `Domain` depends on nothing;
`Application` depends only on `Domain`; `Infrastructure` depends on both and is
where **every** third-party dependency is localized. Nothing depends on
`Infrastructure`.

### Repository

The single abstraction (`IRepository<TAggregate, TKey>`) for loading and adding
aggregates — `GetByIdAsync` and `AddAsync` only; retrieved aggregates are
tracked and their changes flow through the unit of work, and removal is a
soft-delete state change, so there is no `Remove`, `Update`, or `Save`
([ADR-0026](./architecture/decisions/0026-single-repository-contract.md)).
`Infrastructure` provides **generic implementations** for EF Core and for the
Marten-based event store (see
[ADR-0019](./architecture/decisions/0019-event-store-technology-marten.md)). Both
rebuild a stored aggregate through
[reconstitution](#reconstitution) via the aggregate's private parameterless
constructor.

---

## Technology & operations

### .NET Aspire

The **orchestrator** (version 13) used to compose and run the distributed
application locally and in the cloud, via the `AppHost` and `ServiceDefaults`
projects. Aspire is applied at the orchestration layer; the Building Blocks remain
framework-agnostic. See
[ADR-0002](./architecture/decisions/0002-use-dotnet-aspire-13-for-orchestration.md).

### AppHost

The .NET Aspire entry-point project that wires up and runs the services and their
dependencies (e.g. `dotnet run --project src/Aspire/VitalSync.AppHost`).

### Cloud-native

A quality goal for VitalSync: services are built to run in a modern,
containerized, orchestrated cloud environment and to be modular, extensible,
maintainable, testable, and loosely coupled.

### Entity Framework Core (EF Core)

The ORM used for state-stored persistence, running on [PostgreSQL](#postgresql) via
the Npgsql provider (ADR-0020). Persistence tests also use its **InMemory** provider
for fast feedback.

### IClock

An abstraction over "now" that makes time-dependent domain behavior
**deterministic** and testable. The infrastructure's unit of work uses it to mint
the `OccurredAt` carried on each `DomainEventEnvelope` at commit time; the domain
itself takes an `IClock` only where time is a **business** rule.

### Marten

The **MIT-licensed** library that turns [PostgreSQL](#postgresql) into VitalSync's
[event store](#event-store-marten-on-postgresql). Used as a raw event store (append
streams + fetch streams), not through its convention-based aggregation, so the
domain's `LoadFromHistory` rehydration and the ADR-0010/0012 aggregate shape stay
intact. Runs **side-by-side with** [Wolverine](#wolverine) (same ecosystem),
sharing a transaction and outbox (ADR-0023). See
[ADR-0019](./architecture/decisions/0019-event-store-technology-marten.md).

### PostgreSQL

The **single relational database engine** for the platform. It backs **both**
state-stored contexts (EF Core via the Npgsql provider) and event-sourced contexts
([Marten](#marten)), and hosts **both the write and the read database** of every
context
([ADR-0021](./architecture/decisions/0021-write-read-database-pair-per-context.md)).
Standardizing on one engine — already required by Marten (ADR-0019) — minimizes
operational surface and reuses the first-party .NET Aspire PostgreSQL hosting
integration. See
[ADR-0020](./architecture/decisions/0020-postgresql-for-state-stored-contexts.md).

---

## Testing

### Behavior over implementation

A testing principle: assert observable behavior (e.g. "creating a recipe raises a
`RecipeCreated` event") rather than internal details.

### Test categories

- **Unit tests** — a class or method in isolation.
- **Domain tests** — domain rules, invariants, and event-raising on aggregates,
  value objects, and domain events (no framework mocks needed).
- **Application-layer tests** — command/query handlers, dispatcher, pipeline
  behaviors, and `Result` semantics.
- **Persistence tests** — mapping, persistence, and event collection on save.
- **Integration tests** — components working together with real-ish infrastructure.
- **Component communication tests** — gRPC contracts and message publish/consume.

### Testing strategy

Automated tests cover **both** the Building Blocks and the individual
microservices, spanning several categories. See
[Testing strategy](./architecture/testing-strategy.md).

### Tooling

**xUnit** (with built-in `Assert.*`, see
[ADR-0014](./architecture/decisions/0014-replace-fluentassertions-with-xunit-asserts.md)),
**NSubstitute** for substitutes/mocks, and **EF Core InMemory** for fast
persistence tests. Domain tests prefer lightweight hand-written test doubles.

---

## Process & conventions

### "Architecture is fixed, the domain is fluid"

The project's guiding principle: technical decisions (communication mechanisms,
layer separation, architectural patterns) are **mandatory and stable**, while
business/domain requirements are expected to be **refined iteratively**.

### Analyze & challenge

The project phase in which open questions are deliberately left unresolved and
revisited — most notably **which bounded contexts justify Event Sourcing** and
the **final bounded-context decomposition**.

### Architecture Decision Record (ADR)

A lightweight document capturing a single architectural decision, its context, and
its consequences. ADRs are **immutable once accepted** — to change a decision you
add a new ADR that **supersedes** the old one (e.g. ADR-0012 supersedes ADR-0011,
ADR-0023 supersedes ADR-0004).
See the [ADR index](./architecture/decisions/README.md).

### ADR status

An ADR is **Proposed** (under discussion), **Accepted** (decided and in effect),
or **Superseded** (replaced by a later, linked ADR).

### User story

A short, user-centered description of a use case (e.g. *create a recipe*), stored
under [`docs/userStories`](./userStories/nutrition/001_createRecipe.md) and
refined as the domain evolves.
