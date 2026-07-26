# 0021. Write/read database pair per bounded context

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

[ADR-0020](./0020-postgresql-for-state-stored-contexts.md) established
**PostgreSQL** as the single relational engine and mandated **one database per
bounded context**, hosted on a shared server today with a sanctioned
server-per-context migration path.

CQRS separates the **write path** (commands mutating aggregates) from the
**read path** (queries). ADR-0020 left open *where* the read side is stored: the
same tables, a separate schema, or a separate database. For better read/write
scalability — and to let the read model's shape evolve independently of the write
model — we now fix that topology.

Forces:

- **Independent read scaling.** Read and write workloads have different shapes and
  volumes; keeping them in one store couples their scaling and schema evolution.
- **Read models are derived, not authoritative.** A read model is a projection that
  can always be **rebuilt** from the write side (event streams for ES; the
  aggregate state + event log for state-stored). It is therefore safe to hold it
  in a separate, disposable store.
- **Consistency reality.** Two separate PostgreSQL databases cannot share a single
  local transaction, so read-model updates are **necessarily post-commit and
  eventually consistent** (see [ADR-0022](./0022-event-driven-read-models.md)).
- **Boundary integrity (ADR-0020).** Whatever we add must stay **inside** the
  owning context — no database is shared across contexts.

## Decision

**Each bounded context owns exactly two PostgreSQL databases: a write database and
a read database.**

- The **write database** holds the authoritative state: EF Core tables for
  state-stored contexts, and the Marten event streams (`mt_events` / `mt_streams`)
  for event-sourced contexts (see
  [ADR-0019](./0019-event-store-technology-marten.md)).
- The **read database** holds **query-optimized read models** (projections),
  updated from domain events after the write commits (see
  [ADR-0022](./0022-event-driven-read-models.md)).
- Both databases belong to **one bounded context** and are **never shared** with
  another context. Cross-context data flow remains **integration events only**
  (ADR-0004). This refines — does not weaken — ADR-0020's database-per-context rule:
  the unit of ownership is now a **write+read pair**.
- **Topology today:** both databases live on the shared PostgreSQL server. In
  Aspire this is one server resource with **two `AddDatabase(...)` calls per
  context** (e.g. `nutrition-write`, `nutrition-read`), each with its own named
  connection string (e.g. `ConnectionStrings:NutritionWrite`,
  `ConnectionStrings:NutritionRead`).
- **Future migration (sanctioned, non-breaking):** either database of a context may
  later move to its **own dedicated server** — a connection-string change plus a
  data move, touching no Domain/Application/Infrastructure code, exactly as in
  ADR-0020. The read database in particular is a natural candidate to scale out or
  even rebuild from scratch.
- The read database is **derived and rebuildable.** It may be dropped and
  reconstructed by replaying events / re-running projections; it is never the
  system of record.

## Consequences

- **Easier:** read and write stores scale and evolve independently; read schemas
  are free to be denormalized for queries without touching the write model; a
  corrupt or restructured read model can be rebuilt from the write side.
- **Uniform:** ES and state-stored contexts share the same topology — write DB +
  read DB — so there is one mental model regardless of persistence style.
- **Harder / accepted trade-offs:** reads are **eventually consistent** with writes
  (no cross-database transaction is possible); the read model requires a reliable
  update mechanism (ADR-0022) and rebuild tooling; each context now provisions two
  databases and two connection strings instead of one.
- Boundary integrity is preserved: two databases per context, still zero
  cross-context database coupling.

## Alternatives considered

- **Single database per context (read = write tables), per ADR-0020 as-is:**
  simplest and strongly consistent, but couples read/write scaling and schema
  evolution. Rejected as the *default* in favour of a read/write split for
  scalability; still available conceptually for a trivial context, but we
  standardize on the pair for uniformity.
- **Separate read *schema* in the same database (not a separate database):** gives
  read-optimized shapes and keeps a single transaction possible, but does **not**
  deliver independent read scaling or an independently movable/rebuildable read
  store. Rejected because independent scalability was the explicit goal.
- **A different engine for the read store (e.g. a document/search store):**
  possible per-context optimization later, but introduces a second engine against
  ADR-0020's one-engine principle. Rejected for now; PostgreSQL read databases keep
  operations uniform, and a specialized read store can be revisited per context if a
  concrete driver appears.
