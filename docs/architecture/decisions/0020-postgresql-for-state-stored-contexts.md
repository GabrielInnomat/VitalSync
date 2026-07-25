# 0020. PostgreSQL for state-stored contexts; one database per bounded context

- **Status:** Accepted
- **Date:** 2026-07-25

## Context

VitalSync uses **selective Event Sourcing**: EF Core state-storage is the default,
and Event Sourcing is applied only where the event history carries business value
(see [ADR-0012](./0012-optional-event-sourcing-aggregate.md)). The event store was
decided to be **Marten on PostgreSQL** (see
[ADR-0019](./0019-event-store-technology-marten.md)).

Two things were still undefined:

1. **Which relational database engine** the EF Core (state-stored) contexts use.
   The docs said only "EF Core" / "state-stored persistence" without naming a
   provider (SQL Server, MySQL, PostgreSQL, …).
2. **The database topology** for bounded contexts — how many databases and how
   many servers.

The relevant forces:

- **One engine already required.** ADR-0019 commits the platform to PostgreSQL for
  Marten. Introducing a *second* engine (e.g. SQL Server or MySQL) for the
  state-stored contexts would double the operational surface — two engines to run,
  patch, back up, and monitor — for no architectural benefit.
- **Aspire-first.** ADR-0002 commits to .NET Aspire 13, which has a first-party
  PostgreSQL hosting integration (`Aspire.Hosting.PostgreSQL`) modelling a server
  resource with N databases via `AddDatabase(...)`.
- **Independent deployability.** Microservices must not share a database and must
  be migratable independently (see
  [ADR-0004](./0004-asynchronous-messaging-between-services.md)); cross-context
  consistency is achieved via integration events, never shared tables.
- **Architecture is fixed, the domain is fluid.** The *architectural* invariant is
  the logical database-per-context boundary. The *physical* server topology is a
  deployment concern that may evolve as real drivers (scaling, noisy-neighbour
  isolation, data residency, independent failover) emerge.

## Decision

Use **PostgreSQL** as the relational database engine for all state-stored (EF Core)
bounded contexts, via the **`Npgsql.EntityFrameworkCore.PostgreSQL`** provider.

Adopt a **one-engine, database-per-bounded-context** topology:

- **Each bounded context owns its own PostgreSQL database.** Contexts never share a
  database. There are **no cross-database foreign keys, joins, or transactions**;
  cross-context consistency is via integration events (ADR-0004).
- **Today: a single shared PostgreSQL server hosts N databases** — one per context.
  In Aspire this is one `AddPostgres("postgres")` server resource with an
  `AddDatabase(...)` per context.
- Each context has its **own `DbContext`, its own EF Core migrations, and its own
  named connection string** (e.g. `ConnectionStrings:Nutrition`), so a context is
  self-contained and independently migratable.
- **The event store and the state-stored store never co-locate in the same
  database.** Marten's event tables (ADR-0019) and a context's EF Core relational
  tables live in separate databases, even when hosted on the same server, so they
  can be moved and scaled independently.
- **Future migration path (sanctioned, non-breaking):** moving a bounded context's
  database onto its **own dedicated server** ("server per context/service") is an
  explicitly supported evolution. Because each context already has its own
  database, `DbContext`, migrations, and connection string, and because no
  cross-database coupling exists, such a move is a **connection-string change plus a
  data move** — it touches no Domain, Application, or Infrastructure code.

Testing is unchanged: EF Core **InMemory** remains the provider for fast unit and
persistence tests (see [ADR-0014](./0014-replace-fluentassertions-with-xunit-asserts.md)
and the testing strategy). PostgreSQL-backed integration tests (e.g. via
Testcontainers) are a possible future addition and do not change this decision.

## Consequences

- **Easier:** A single database engine (PostgreSQL) backs both the Marten event
  store and the EF Core state-stored contexts — one engine to run, patch, back up,
  monitor, and learn, and one first-party Aspire integration. Logical isolation
  (database-per-context) preserves independent deployability and keeps
  cross-context consistency on the integration-event backbone.
- **Reversible topology:** Because the architectural rule is the logical
  database-per-context boundary — not the server count — evolving from "one server,
  N databases" to "server per context" is a config/ops change with no code impact,
  done incrementally per context when a real driver appears.
- **Harder / accepted trade-offs:** A shared server means shared operational fate
  (a server outage affects all co-located contexts) and potential noisy-neighbour
  effects under load. These are accepted in early development and are precisely the
  drivers that would justify invoking the server-per-context migration path later.
- Provider-specific concerns (Npgsql type mapping, PostgreSQL-specific column
  types) are localized to each context's Infrastructure/persistence layer; the
  Domain and Application layers stay persistence-ignorant (ADR-0012, ADR-0018).

## Alternatives considered

- **SQL Server for state-stored contexts:** mature EF Core support, but introduces
  a **second database engine** alongside the PostgreSQL required by Marten, doubling
  operational surface and (for some editions) adding licensing cost. Rejected — no
  architectural benefit over standardizing on PostgreSQL.
- **MySQL / MariaDB for state-stored contexts:** same objection — a second engine to
  operate next to PostgreSQL, with a weaker first-party Aspire story. Rejected.
- **Stay provider-agnostic / defer the choice:** leaves an ambiguous operational
  story now that PostgreSQL is already mandated by ADR-0019, and invites
  inconsistent per-context choices. Rejected — the platform benefits from one
  explicit, standard engine.
- **Server per microservice now:** maximal physical isolation, but a premature
  operational commitment (more infrastructure to run and monitor) before any
  concrete driver exists. Rejected for now — preserved as an explicit, non-breaking
  future migration path instead, since the logical database-per-context boundary
  already makes it cheap to adopt later.
- **Co-locate Marten and EF Core in one database per service:** fewer databases, but
  couples the event store and the relational store so they cannot move or scale
  independently. Rejected — separate databases keep the two stores independently
  evolvable.
