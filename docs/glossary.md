# Glossary

A reference for the vocabulary used across VitalSync — both the **business
concepts** (what the product does) and the **technical concepts, patterns, and
architecture** (how it is built). It is written to be worth reading for
experienced engineers who want to understand VitalSync's use cases or the
design decisions behind it.

Terms are grouped by theme. Within each group they are ordered so that
foundational terms come first. Cross-references point to the deeper documents
under [`docs/architecture`](./architecture/overview.md) and the
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

### VitalSync

The product: a **cloud-native, distributed platform** that unifies **nutrition**,
**fitness**, and **health analytics** behind a single user experience. Users
manage diet and workout data and derive insights from it. A guiding principle of
the project is *"the architecture is fixed, the domain is fluid"* — technical
decisions are mandatory and stable, while business requirements are refined
iteratively.

### Nutrition

The business domain concerned with food and diet. Its use cases include managing
**ingredients** and their nutritional values, creating **recipes**, composing
**meal plans**, generating shopping lists, and calculating **nutrient intake**
from consumed meals. It is the most developed domain in the current
[user stories](./userStories/nutrition/001_createRecipe.md).

### Fitness

The business domain concerned with physical activity. Its use cases include
managing **exercises**, creating **workout plans**, tracking completed **workout
sessions**, and determining energy expenditure / calories burned.

### Analytics & Reporting

The business domain that derives insights and reports from the data produced by
Nutrition and Fitness. Its concrete requirements are intentionally identified and
extended as the project evolves.

### Ingredient

A Nutrition concept: a single food item together with its nutritional values. The
ingredient catalog is largely CRUD-shaped and is therefore a candidate for
traditional (EF Core) persistence rather than Event Sourcing.

### Recipe

A Nutrition **aggregate** composed of ingredients and quantities. It is the
canonical example used throughout the architecture docs (e.g. `Recipe`,
`RecipeId`, `RecipeCreated`) to illustrate aggregates, strongly typed
identifiers, and domain events.

### Meal plan

A Nutrition concept that composes recipes/meals over time; the basis for shopping
lists and for calculating nutrient intake.

### Workout session

A Fitness concept representing one completed training session (started, exercises
logged, completed). Because it forms a natural event stream and its full history
has analytical value, it is a **candidate** for Event Sourcing (still to be
decided — see [CQRS & Event Sourcing](./architecture/cqrs-and-event-sourcing.md)).

---

## Bounded contexts & the ubiquitous language

### Domain-Driven Design (DDD)

The overarching design approach. VitalSync applies DDD in every microservice:
the **domain layer is the heart** of each service, models the business explicitly,
and is kept independent of infrastructure. See
[Domain model](./architecture/domain-model.md).

### Bounded Context

A boundary within which a domain model and its **ubiquitous language** are
consistent and self-contained. VitalSync's candidate contexts are **Nutrition**,
**Fitness**, and **Analytics**; the final decomposition is deliberately part of
the project and refined iteratively. Contexts never share a domain model.

### Ubiquitous Language

The shared, precise vocabulary used consistently by domain experts and code
within a bounded context. This glossary is, in part, an entry point into that
language.

### Microservice

An independently deployable service that owns one business area and its data.
Services do **not** share databases and avoid synchronous coupling, which
preserves independent deployability. VitalSync has one service per business area
(Nutrition, Fitness, Analytics).

---

## Tactical Domain-Driven Design

These are the building-block primitives provided by
[`BuildingBlocks.Domain`](./architecture/building-blocks-domain.md) and used by
every service's domain layer.

### Entity

An object with an **identity** that persists over time. Equality is based on
identity — two entities are equal when they are the **same concrete type** and
have the **same id** (not on their attribute values). See
[ADR-0008](./architecture/decisions/0008-entity-identity-and-equality.md).

### Value Object

An immutable object defined entirely by its attributes, with **structural
equality**. Examples in VitalSync: a nutritional value, a quantity with a unit, a
calorie amount.

### Aggregate

A cluster of domain objects treated as a single unit for data changes, bounded by
its **Aggregate Root**.

### Aggregate Root

The consistency boundary and single entry point for an aggregate. It exposes
**behavior (not setters)** to enforce invariants, **raises domain events** to
announce business-relevant changes, and exposes those events **read-only** to
other layers. VitalSync provides two aggregate bases:

- `AggregateRoot<TKey>` — for **state-stored** (EF Core) aggregates; identity is
  passed to the constructor; records events via `AddDomainEvent`.
- `EventSourcedAggregateRoot<TKey, TState>` — for **event-sourced** aggregates;
  identity is derived from state; records events via `RaiseEvent`.

The author picks the base that matches the service's persistence strategy
([ADR-0012](./architecture/decisions/0012-optional-event-sourcing-aggregate.md)).

### Strongly Typed Identifier

An aggregate identifier modeled as a **Value Object** (a `readonly record struct`
implementing `IEntityKey<TValue>`) rather than a raw primitive. A `RecipeId` and
an `IngredientId` are distinct, incompatible types even though both wrap a `Guid`,
so passing the wrong id is a **compile-time error**. Each key defines its own
`IsEmpty` rule, making identity validation type-agnostic. See
[ADR-0005](./architecture/decisions/0005-strongly-typed-aggregate-identifiers.md).

### State object (`IState<TSelf, TKey>`)

For event-sourced aggregates, an **immutable** object that owns the aggregate's
identity and all **apply / evolve** logic. `Apply(IDomainEvent)` returns the next
state (`this with { … }`). Keeping evolution logic on the state keeps large
aggregates free of "apply noise" — the aggregate class holds only the public
command API. See
[ADR-0010](./architecture/decisions/0010-aggregate-state-object.md).

### Domain Event

A record of something **business-relevant** that has happened in the domain (e.g.
`RecipeCreated`, `RecipeRenamed`). Domain events are **pure business data** —
no infrastructure or third-party types — carry a stable `EventId` and an
`OccurredAt`, and are **internal** to a service. They may be translated into an
**integration event** at the service boundary.

### Aggregate owns its domain events

A core ownership rule: **only** the aggregate may raise events; **only** a
privileged infrastructure contract may clear them; everyone else gets a
**read-only** view. This is enforced structurally via two interfaces —
`IHasDomainEvents` (read-only, everyone) and `IDomainEventsManager` (clear,
infrastructure-only, implemented explicitly). See
[ADR-0006](./architecture/decisions/0006-aggregate-owns-domain-events.md) and
[ADR-0007](./architecture/decisions/0007-read-only-vs-managed-domain-events.md).

### Business Rule vs. Domain Validation

Two distinct concepts, each with its own rule interface and exception:

- **Business rule** (invariant) — `IBusinessRule.IsBroken()`, throws
  `BusinessRuleViolationException`.
- **Domain validation** (input constraint) — `IDomainValidationRule.IsInvalid()`,
  throws `DomainValidationException`.

`RuleChecker` evaluates them and throws the matching exception. See
[ADR-0009](./architecture/decisions/0009-business-rules-and-domain-validation.md).

---

## CQRS & the application layer

### CQRS (Command Query Responsibility Segregation)

A **mandatory** pattern in every microservice: write operations (**commands**) are
separated from read operations (**queries**), each handled by a dedicated handler.
The abstractions live in
[`BuildingBlocks.Application`](./architecture/building-blocks-application.md).

### Command

An object that expresses **intent** and **changes state** (e.g. `CreateRecipe`,
`CompleteWorkoutSession`). Marked with `ICommand` (returns `Task<Result>`) or
`ICommand<TResult>` (returns `Task<Result<TResult>>`). By convention, **create**
commands return the new aggregate's strongly typed id; **delete/update** commands
return a plain `Result`.

### Query

An object that **reads state and never mutates it**. Marked with `IQuery<TResult>`
and handled by an `IQueryHandler<TQuery, TResult>` returning `Task<Result<TResult>>`.

### Handler

The single dedicated class that processes one command or query
(`ICommandHandler<…>`, `IQueryHandler<…>`). Handlers are **async-only** —
they return a `Task<…>` and accept a `CancellationToken`.

### Dispatcher (`ISender`) / hand-rolled mediator

The single entry point callers use to send a command or query. VitalSync uses a
**hand-rolled** mediator instead of MediatR: the `ISender` contract lives in
`Application`, its DI-based implementation in `Infrastructure` resolves the
matching handler and pipeline. See
[ADR-0015](./architecture/decisions/0015-hand-rolled-cqrs-mediator.md).

### Pipeline behavior

A wrapper around handler execution that applies **cross-cutting concerns**
(exception translation, logging, unit-of-work, validation). Behaviors run in
**explicit registration order**; only the `IPipelineBehavior<TRequest, TResponse>`
contract lives in `Application`, the concrete behaviors live in `Infrastructure`.

### Result / Result&lt;T&gt;

The uniform outcome model returned by handlers. `Result` is success or a failure
carrying one or more `Failure`s; `Result<T>` additionally carries a value on
success. It lives in `BuildingBlocks.Application`. See
[ADR-0016](./architecture/decisions/0016-remove-common-result-in-application.md).

### Failure / FailureCategory

A structured error: a stable machine-readable `Code` (e.g. `recipe.name_required`),
a human-readable `Message`, and a `Category`. Categories are `Validation`,
`BusinessRule`, `NotFound`, and `Conflict`. There is deliberately **no**
`Unexpected` category — unexpected errors stay exceptions and bubble to a thin
global handler.

### Exception-to-Result translation

The rule that expected **domain exceptions** (`BusinessRuleViolationException`,
`DomainValidationException`) are translated into `Result.Failure` by an
`ExceptionToResultBehavior` at the application boundary, while unexpected
exceptions bubble up. See
[ADR-0017](./architecture/decisions/0017-application-error-handling-and-result.md).

---

## Event Sourcing & persistence

### Persistence strategy (selective Event Sourcing)

VitalSync uses **two complementary approaches**. Traditional state-storage
(**EF Core**) is the default; **Event Sourcing** is applied *only where it adds
business value*. The exact contexts that justify Event Sourcing are still to be
decided. See [CQRS & Event Sourcing](./architecture/cqrs-and-event-sourcing.md).

### Event Sourcing (ES)

A persistence approach in which an aggregate's state is derived from an
**append-only stream of domain events** rather than being stored directly. It
provides inherent audit/history and natural temporal queries / replay, at the
cost of higher complexity (event store, projections, versioning).

### State-stored (traditional) persistence

The default approach: the aggregate object is persisted directly via EF Core.
Simpler than Event Sourcing, but without inherent history or replay.

### RaiseEvent / LoadFromHistory

The two ways an event-sourced aggregate's state changes: `RaiseEvent(e, clock)`
stamps a new event, applies it to the state, validates identity, advances the
version, and records it; `LoadFromHistory(history)` **replays** a persisted stream
to rebuild state (rehydration, recording nothing). A **replay-misuse guard**
prevents `LoadFromHistory` from running after uncommitted events exist.

### Rehydration

Rebuilding an event-sourced aggregate's current state by replaying its event
stream (via `LoadFromHistory`), typically inside the event-sourced repository.

### Version (stream position)

The monotonic position of an event-sourced aggregate within its stream, advanced
on every `RaiseEvent`. Used for ordering and optimistic **concurrency** (asserted
as the expected revision when appending to the Marten event stream).

### Projection / Read model

A representation of data **optimized for queries** on the read side of CQRS. With
Event Sourcing, projections are built by replaying events; with EF Core, read
models may be the same tables or purpose-built views.

### Event store (Marten on PostgreSQL)

The technology backing event-sourced contexts: **Marten** (MIT-licensed) on
**PostgreSQL**, used as a **raw event store**. The event-sourced repository
appends uncommitted domain events to the stream (with optimistic concurrency on
`Version`) and, on load, fetches the raw stream and folds it through the
aggregate's own `LoadFromHistory` — Marten's convention-based `Apply`-on-aggregate
aggregation is **not** used, keeping the domain untouched. Snapshotting is deferred
but can be added per context without an event-schema migration. PostgreSQL has a
first-party .NET Aspire hosting integration. See
[ADR-0019](./architecture/decisions/0019-event-store-technology-marten.md).

### Snapshotting

An optional optimization for event-sourced aggregates: persist the aggregate's
`State` at a known version so a load can start from the snapshot and replay only
the tail of the stream instead of the whole history. In VitalSync it is
**deferred**; because a Marten snapshot is a separate document and the event schema
is unchanged, it can be introduced later per context with **no event migration**.

### Entity Framework Core (EF Core)

The ORM used for state-stored persistence. Persistence tests also use its
**InMemory** provider for fast feedback.

### Unit of Work

The infrastructure component that groups changes into a single transactional
save. On save, it collects the aggregates' domain events, hands them to the
dispatcher/outbox, and clears them **only after** the save succeeds.

---

## Messaging & communication

### Backend-for-Frontend (BFF)

The single entry point for the frontend. It exposes **REST** to the Blazor client
and orchestrates calls to microservices via **code-first gRPC**. It centralizes
cross-cutting concerns (auth, aggregation, response shaping) and decouples the UI
from the service topology. See
[ADR-0003](./architecture/decisions/0003-bff-with-rest-and-code-first-grpc.md).

### Frontend (Blazor)

The user interface. It holds **no business logic** and communicates
**exclusively** through the BFF — never directly with a microservice.

### Code-first gRPC

The synchronous, strongly typed RPC used between the BFF and microservices, with
contracts defined **in C#** rather than hand-authored `.proto` files.

### Communication rules

The mandatory topology: **Frontend → BFF** (REST) → **Microservice** (gRPC), and
**Microservice ↔ Microservice** asynchronous **only**. There is no direct
synchronous service-to-service communication. See
[Communication](./architecture/communication.md).

### Integration Event

A message published to the messaging backbone to communicate **across services**.
Distinct from a **domain event** (internal to one service); domain events are
translated into integration events at the service boundary.

### Asynchronous messaging

The only channel for inter-service communication, chosen to maximize loose
coupling and independent deployability and to prevent distributed call chains and
temporal coupling. See
[ADR-0004](./architecture/decisions/0004-asynchronous-messaging-between-services.md).

### RabbitMQ

The chosen messaging platform (the message broker) for asynchronous inter-service
communication.

### MassTransit

The abstraction layer over RabbitMQ, providing publish/subscribe, the
**transactional outbox**, retries, and dead-lettering.

### Outbox (transactional outbox)

A reliability pattern: domain events collected on save are written and then
forwarded to the messaging backbone, ensuring messages are not lost even if the
process fails after committing state.

---

## The Building Blocks platform

### Building Blocks

A **reusable platform of shared primitives** that underpins the microservices
**without coupling them to VitalSync**, so it can be reused in future projects.
Split into exactly **three** packages by a **purity / dependency** boundary, not a
functional one. See [Building Blocks](./architecture/building-blocks.md) and
[ADR-0018](./architecture/decisions/0018-three-building-block-packages.md).

### BuildingBlocks.Domain

The **pure core**: tactical DDD primitives (entities, the two aggregate bases,
strongly typed keys, domain events, business-rule/validation abstractions). It has
**zero** third-party dependencies — BCL only. See
[reference](./architecture/building-blocks-domain.md).

### BuildingBlocks.Application

The **framework-agnostic use-case layer**: CQRS contracts, the pipeline-behavior
and dispatcher contracts, and the `Result` / `Failure` model. Depends **only** on
`Domain` and holds **contracts, not implementations**. See
[reference](./architecture/building-blocks-application.md).

### BuildingBlocks.Infrastructure

The **single outer layer** holding all reusable, framework-bound,
third-party-backed implementations that are still VitalSync-agnostic: unit of work,
generic repositories (EF Core and the Marten-based event store, see
[ADR-0019](./architecture/decisions/0019-event-store-technology-marten.md)), domain-
and integration-event dispatching, the RabbitMQ/MassTransit transport, and the
DI-based CQRS dispatcher and pipeline behaviors. Depends on both `Domain` and
`Application`.

### Purity boundary

The rule that decides where code belongs: `Domain` depends on nothing;
`Application` depends only on `Domain`; `Infrastructure` depends on both and is
where **every** third-party dependency is localized. Nothing depends on
`Infrastructure`.

### Repository

An abstraction for loading and saving aggregates. `Infrastructure` provides
**generic repositories** for EF Core and for the Marten-based event store (see
[ADR-0019](./architecture/decisions/0019-event-store-technology-marten.md)).

---

## Technology & operations

### Cloud-native

A quality goal for VitalSync: services are built to run in a modern,
containerized, orchestrated cloud environment and to be modular, extensible,
maintainable, testable, and loosely coupled.

### .NET Aspire

The **orchestrator** (version 13) used to compose and run the distributed
application locally and in the cloud, via the `AppHost` and `ServiceDefaults`
projects. Aspire is applied at the orchestration layer; the Building Blocks remain
framework-agnostic. See
[ADR-0002](./architecture/decisions/0002-use-dotnet-aspire-13-for-orchestration.md).

### Marten

The **MIT-licensed** library that turns **PostgreSQL** into VitalSync's event
store. Used as a raw event store (append streams + fetch streams), not through its
convention-based aggregation, so the domain's `LoadFromHistory` rehydration and the
ADR-0010/0012 aggregate shape stay intact. See
[ADR-0019](./architecture/decisions/0019-event-store-technology-marten.md).

### AppHost

The .NET Aspire entry-point project that wires up and runs the services and their
dependencies (e.g. `dotnet run --project src/Aspire/VitalSync.AppHost`).

### IClock

An abstraction over "now" that makes time-dependent domain behavior
**deterministic** and testable. On the event-sourced base, `RaiseEvent` stamps
events through `IClock`.

---

## Testing

### Testing strategy

Automated tests cover **both** the Building Blocks and the individual
microservices, spanning several categories. See
[Testing strategy](./architecture/testing-strategy.md).

### Test categories

- **Unit tests** — a class or method in isolation.
- **Domain tests** — domain rules, invariants, and event-raising on aggregates,
  value objects, and domain events (no framework mocks needed).
- **Application-layer tests** — command/query handlers, dispatcher, pipeline
  behaviors, and `Result` semantics.
- **Persistence tests** — mapping, persistence, and event collection on save.
- **Integration tests** — components working together with real-ish infrastructure.
- **Component communication tests** — gRPC contracts and message publish/consume.

### Tooling

**xUnit** (with built-in `Assert.*`, see
[ADR-0014](./architecture/decisions/0014-replace-fluentassertions-with-xunit-asserts.md)),
**NSubstitute** for substitutes/mocks, and **EF Core InMemory** for fast
persistence tests. Domain tests prefer lightweight hand-written test doubles.

### Behavior over implementation

A testing principle: assert observable behavior (e.g. "creating a recipe raises a
`RecipeCreated` event") rather than internal details.

---

## Process & conventions

### Architecture Decision Record (ADR)

A lightweight document capturing a single architectural decision, its context, and
its consequences. ADRs are **immutable once accepted** — to change a decision you
add a new ADR that **supersedes** the old one (e.g. ADR-0012 supersedes ADR-0011).
See the [ADR index](./architecture/decisions/README.md).

### ADR status

An ADR is **Proposed** (under discussion), **Accepted** (decided and in effect),
or **Superseded** (replaced by a later, linked ADR).

### "Architecture is fixed, the domain is fluid"

The project's guiding principle: technical decisions (communication mechanisms,
layer separation, architectural patterns) are **mandatory and stable**, while
business/domain requirements are expected to be **refined iteratively**.

### Analyze & challenge

The project phase in which open questions are deliberately left unresolved and
revisited — most notably **which bounded contexts justify Event Sourcing** and
the **final bounded-context decomposition**.

### User story

A short, user-centered description of a use case (e.g. *create a recipe*), stored
under [`docs/userStories`](./userStories/nutrition/001_createRecipe.md) and
refined as the domain evolves.
