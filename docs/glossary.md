# Glossary

The vocabulary used across VitalSync's documentation, defined once so the other documents can use it
without re-explaining it.

Entries here are **cross-cutting**: terms that mean the same thing everywhere in the system. Business
vocabulary is deliberately not collected here, because a ubiquitous language belongs to its bounded
context — *Ingredient* means one thing in [Nutrition](./domains/nutrition.md) and something else
wherever else it appears. Each domain document therefore defines its own terms.

Terms are grouped by theme and ordered alphabetically within a theme. The definitions are short on
purpose; the reasoning lives in [Architecture](./architecture.md), [Patterns](./patterns.md) and
[Technologies](./technologies.md).

## Strategic design

### Bounded Context

The boundary within which a model and its vocabulary are consistent. In VitalSync a bounded context
is the unit of everything: one context, one service, one database pair, one deployable.

### Domain-Driven Design (DDD)

Modeling software around the business domain and its language rather than around technical layers.
VitalSync uses both its strategic side (bounded contexts, ubiquitous language) and its tactical side
(aggregates, entities, value objects, domain events).

### Microservice

An independently deployable service owning one bounded context and its data, communicating with
other services only asynchronously.

### Ubiquitous Language

The shared vocabulary of a bounded context, used identically by domain experts, documentation and
code. It is valid inside its context only.

## Tactical design

### Aggregate

A cluster of entities and value objects treated as one unit for consistency. It is loaded, changed
and saved as a whole, and it is the transactional boundary: one transaction changes one aggregate.

### Aggregate Root

The single entity through which an aggregate is accessed. Nothing outside the aggregate holds a
reference to anything but the root, and every change goes through it.

### Business Rule

An invariant the domain enforces, expressed as a named concept rather than as an inline condition,
so that a violation can be reported as a specific failure rather than as a generic error.

### Domain Event

A fact that happened inside the domain, named in the past tense (`RecipeCreated`). It is raised by
the aggregate that caused it and stays inside the bounded context.

### Domain Validation

Checking that input is structurally usable at all — a required value is present, a number is in
range. It is distinct from a business rule, which concerns meaning rather than form.

### Entity

An object with an identity that persists across changes. Two entities with equal attributes but
different identifiers are different entities.

### Strongly Typed Identifier

An identifier with its own type (`RecipeId` rather than `Guid`), so that the compiler rejects
passing one kind of identifier where another is expected.

### Value Object

An object defined entirely by its values, without identity. It is compared by value and treated as
immutable.

## Application layer

### Command

An instruction to change state. It is handled by exactly one handler and returns success or failure,
not data.

### CQRS

Command Query Responsibility Segregation: separating the model that changes state from the model
that reads it, so that each can be shaped for its own job.

### Failure

The description of why an operation did not succeed, carrying a category that determines how it is
translated at the edge — for example into an HTTP status code.

### Handler

The application-layer class that executes one command or one query. It orchestrates; it does not
contain business rules.

### Query

A request for data. It does not change state and it reads from the read side.

### Result

The return type of the application layer, expressing success or failure explicitly instead of
signalling failure with exceptions. Expected failures are values; exceptions stay exceptional.

## Persistence

### Event Sourcing

Storing the sequence of events that happened instead of the current state, and deriving state by
replaying them. The event log is the source of truth.

### Eventual consistency

The read side may briefly lag behind the write side, because it is updated after the write
transaction has committed rather than inside it.

### Event store

The append-only storage of event streams used by event-sourced contexts.

### Persistence strategy

The choice between state-stored and event-sourced persistence. It is made **per bounded context**,
never per aggregate.

### Projection

The process that turns events into a read model, and by extension the handler that does it. It must
tolerate being run again over events it has already seen.

### Read model

A shape of data built for a specific read, stored in the read database and updated from events.

### Rebuild

Discarding a read model and reconstructing it from the events, used when a projection changes or a
read model is found to be wrong.

### Snapshotting

Periodically storing the derived state of an event stream so that replay does not have to start from
the beginning. Not used in VitalSync today.

### State-stored persistence

Storing the current state directly, in the traditional way, and updating it in place.

### Unit of Work

The boundary that groups the changes of one operation, commits them in a single transaction and
hands the resulting domain events on afterwards.

### Version

The position of an aggregate in its history, used to detect concurrent modification: a write that
expects a version other than the current one is rejected.

### Write database / Read database

The pair of databases each bounded context owns — one for changing state, one for reading it. No
database is shared between contexts, and no query spans two of them.

## Communication

### Asynchronous messaging

Communication by publishing messages a broker delivers, without the sender waiting for the receiver.
It is the only way services communicate with each other.

### Backend-for-Frontend (BFF)

The single backend the frontend talks to. It exposes REST outwards and calls the services over gRPC
inwards, and it holds no business logic.

### Code-first gRPC

Defining gRPC contracts as C# types instead of hand-written `.proto` files, so the contract lives
next to the code that uses it.

### Integration Event

A fact published beyond a bounded context's boundary for other contexts to consume. It is a
deliberate, versioned contract and is not the same thing as a domain event.

### Outbox

Writing outgoing messages into the same transaction as the state change, and delivering them only
after that transaction commits. It is what makes "state changed" and "event published" inseparable.

### Platform exchange

The single broker exchange all integration events are published to, from which each consuming
context binds its own queue.

### Publisher

The component that takes the domain events collected during a unit of work, maps the ones that are
meant to leave the context into integration events, and enqueues them in the outbox.

## Runtime

### AppHost

The Aspire project that composes the distributed system — services, databases, broker, frontend —
and defines their dependencies and start-up order.

### Cloud-native

Built to run as independently deployable, individually scalable services against managed
infrastructure, observable from the outside.

### Migration worker

The short-lived process that applies a context's schema migrations and then exits. Its service does
not start before it has completed.

### Service defaults

The shared wiring every host applies: observability, health checks, resilience and service
discovery, so that no service configures these on its own.

### Thessera

The external building-block platform VitalSync is built on, consumed as the `GaWeCodes.Thessera.*`
packages. It provides the domain, application, persistence and messaging foundations; it is
developed and documented in its own repository.
