# Architecture

VitalSync is a cloud-native, distributed application. This document describes how the system is
put together as a whole: the tiers it consists of, the rules that govern how they talk to each
other, and how the pieces are composed at runtime.

The patterns applied inside a service are described in [Patterns](./patterns.md), and the products
those patterns run on in [Technologies](./technologies.md).

## Logical tiers

VitalSync consists of four tiers:

1. **Frontend (Blazor)** — the user interface. It holds no business logic.
2. **Backend-for-Frontend (BFF)** — the single entry point for the frontend. It exposes REST
   outward and calls the microservices inward.
3. **Microservices** — one per business area. Each one is independent and owns its data.
4. **Messaging backbone** — the only channel between services, strictly asynchronous.

```text
Blazor ──REST──> BFF ──gRPC──> Microservices ──async messaging──> Microservices
```

## Communication rules

These rules are mandatory.

1. **The frontend talks to the BFF and to nothing else.** It never calls a microservice directly.
   This keeps the UI independent of the service topology and gives cross-cutting concerns such as
   authentication, aggregation and response shaping a single place to live.
2. **The BFF exposes REST to the frontend.** HTTP and JSON, consumed by the Blazor client.
3. **The BFF talks to the microservices via code-first gRPC.** Contracts are written in C# rather
   than in hand-authored `.proto` files, so they stay next to the code that consumes them.
4. **Microservices communicate asynchronously only.** There is no synchronous service-to-service
   call anywhere in the system. Everything goes through the messaging backbone, which is what keeps
   the services independently deployable and free of distributed call chains.

A service never reads another context's database either. Cross-context data arrives exclusively as
an [integration event](./patterns.md#integration-events).

## Service anatomy

Every microservice is built the same way, and dependencies point inward: the domain knows nothing
about the application layer, which knows nothing about infrastructure. A contract lives in the
innermost layer that consumes it.

- **Domain** — aggregates, entities, value objects and domain events. No infrastructure, no
  third-party types.
- **Application** — commands, queries and their handlers, plus the pipeline around them.
- **Infrastructure** — persistence, messaging and the wiring that binds the layers to real
  products.
- **Api** — the gRPC endpoint the BFF calls, plus health endpoints.

Alongside each service sits a **migration worker**: a short-lived host that brings the context's
databases to the current schema and then exits. The service waits for it to finish before starting.

The building blocks these layers rest on — the aggregate bases, the dispatcher, the unit of work,
the outbox — are not part of VitalSync. They come from the external
[Thessera](./technologies.md#thessera) packages.

## Database topology

**Each bounded context owns exactly two databases: a write database and a read database.** They are
never shared with another context.

- The **write database** holds the authoritative state.
- The **read database** holds query-optimized read models. It is derived and rebuildable, and never
  the system of record.

There are no foreign keys, joins or transactions across databases. Consistency between contexts is
achieved through integration events, not through the database.

Today all databases live on one shared PostgreSQL server. Moving a context's databases onto a
dedicated server later is a supported, non-breaking migration: a connection-string change and a data
move, touching no application code.

## Runtime composition

The system is composed and run by the .NET Aspire AppHost in `src/Aspire/VitalSync.AppHost`:

- **`messaging`** — the RabbitMQ broker, with the management plugin and a data volume.
- **`postgres`** — the single relational engine, with pgAdmin and a data volume.
- **`<context>-write` and `<context>-read`** — the database pair each context owns.
- **`<context>-migration-service`** — the migration worker, which runs to completion before its
  service starts.
- **`nutrition-service`, `fitness-service`, `health-analytics-service`** — the microservices, each
  gated on a `/health` check.
- **`backend-for-frontend`** — fans out to the three services.
- **`web-frontend`** — the Blazor client, and the only externally reachable endpoint.

Adding a bounded context means adding this same set: two databases, one migration worker, one
service. The services and migration workers currently exist as skeletons without domain code.

Every service host wires the same defaults: service defaults and telemetry, a readiness check per
database it owns, a readiness check for the broker, problem-details error handling with a thin
global exception handler, and the default health endpoints. The connection names are the Aspire
resource names.

## Principles

- **Domain-Driven Design** in every microservice.
- **CQRS** in every microservice, separating the write side from the read side.
- **Event Sourcing where it earns its keep**, traditional persistence everywhere else.
- **Clear separation of business domains** — no domain model is shared across a context boundary.
- **Independent deployability** — no shared databases, no synchronous coupling.

The architecture is meant to be modular, extensible, maintainable, testable, loosely coupled and
cloud-native. A guiding principle of the project is that the architecture is fixed while the domain
is fluid: technical decisions are stable and mandatory, business requirements are refined
iteratively.
