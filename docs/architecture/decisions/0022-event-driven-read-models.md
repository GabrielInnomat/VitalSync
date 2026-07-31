# 0022. Event-driven read models via an outbox-backed publisher

- **Status:** Accepted
- **Date:** 2026-07-26
- **Amended:** 2026-07-31 (integration-event publication is bound to the handler's message context — see the note below)

## Context

[ADR-0021](./0021-write-read-database-pair-per-context.md) gives every bounded
context a **write database** and a **read database**. The read database holds
query-optimized **read models** (projections) that must be kept up to date from the
write side. This applies **uniformly** to both persistence styles:

- **Event-sourced** contexts (Marten raw store, ADR-0019) produce a stream of
  **domain events** on every change.
- **State-stored** contexts (EF Core, ADR-0020) also raise **domain events** on
  their aggregates (ADR-0006); the aggregate owns them and they are collected on
  save.

So both styles already emit domain events — the natural, uniform trigger for
updating a read model.

The decisive constraint comes from ADR-0021: the write DB and read DB are
**separate PostgreSQL databases**, so a **single local transaction cannot span
both**. Read-model updates are therefore **necessarily post-commit and eventually
consistent**. The real design question is not *sync vs async* (async is forced) but
**in-memory dispatch vs durable dispatch**:

- A purely **in-memory** publisher that updates the read DB right after the write
  commit has a **reliability gap**: if the process crashes after the write commit
  but before the read-DB update, that read model **silently and permanently
  drifts**.
- A **durable** (outbox-backed) publisher records the events in the write
  transaction and retries delivery until each projection succeeds, closing that gap.

VitalSync already requires a **transactional outbox** for reliable integration-event
publishing to RabbitMQ (ADR-0004). Reusing that same outbox for read-model
projections yields **one mechanism** for both concerns.

## Decision

Update read models with an **event-driven, outbox-backed Publisher**, used
uniformly for event-sourced and state-stored contexts.

**Flow:**

1. On command handling, the aggregate raises **domain events** (ES: appended to the
   Marten stream; state-stored: collected on the aggregate).
2. In the **write transaction**, those domain events are also written to a
   **transactional outbox** in the write database. Because this is the same local
   transaction as the state/stream write, the events are captured atomically with
   the change (no cross-database transaction required).
3. After commit, the **Publisher** (in `BuildingBlocks.Infrastructure`) **drains the
   outbox** and dispatches each event to:
   - **in-context projection handlers**, which update the context's **read
     database** (ADR-0021); and
   - the **integration-event path**: domain events selected for cross-service
     communication are translated to **integration events** and published to
     **RabbitMQ/MassTransit** (ADR-0004).
4. An outbox entry is marked processed **only after** its handlers succeed; on
   failure or crash it is **retried**, giving **at-least-once** delivery.

> **Implementation note (amendment 2026-07-31):** Empirical verification (IMP-04 in
> `Improvements.md`) showed that resolving the integration-event transport from DI
> inside the envelope handler produced a **fresh, un-enrolled message context** —
> integration events left the process immediately, outside the inbox transaction
> (duplicates on redelivery) and without correlation propagation. The
> integration-event path is therefore **bound to the handler's own message
> context**: Wolverine injects `IMessageContext` into `DomainEventEnvelopeHandler`
> as a handler parameter, and the `Publisher` receives the resulting
> `IIntegrationEventSink` **explicitly as a parameter** (contract in
> `BuildingBlocks.Application`; `WolverineIntegrationEventSink` /
> `NullIntegrationEventSink` in Infrastructure). The former DI-resolved
> `IIntegrationEventTransport` abstraction is removed. Step 4's "only after its
> handlers succeed" now demonstrably includes integration events: they are held
> back when a projection handler fails and only leave with the inbox transaction's
> success. Additionally, both unit-of-work implementations flush the outbox
> **immediately after a successful commit** (verified end-to-end in
> `OutboxFlushOnCommitTests`); for EF Core hosts, `UseEfCorePersistence` registers
> Wolverine's PostgreSQL-backed durable message store on the write database at
> composition time, which the EF outbox requires.

**Consequences of at-least-once — mandatory handler rules:**

- **Idempotent projection handlers.** Applying the same event twice must produce the
  same read-model state (e.g. upsert by key; ignore already-applied events).
- **Per-aggregate ordering.** Events for a single aggregate are applied in
  order; each read model tracks a **last-processed position/version** (per aggregate
  or per stream) and skips events at or below it. Cross-aggregate ordering is **not**
  guaranteed and must not be relied upon.

**Consistency:** eventual, but **low-latency** — the Publisher drains immediately
after commit in the happy path, so the read model typically trails the write by a
very short interval. Correctness never depends on that interval being zero.

**Domain vs integration events (unchanged, ADR-0004 / communication.md):** domain
events are **internal** to a context and drive **in-context** projections;
**integration events** are the **only** cross-context signal. A read model is
updated **only** by its **own** context's domain events (in-context projections) or,
when it must reflect data owned elsewhere, by **integration events** that context
subscribes to — never by directly reading another context's database.

**Placement (ADR-0018):**

- **`BuildingBlocks.Application`** — contracts only: the domain-event **publisher**
  and **projection-handler** abstractions, and the integration-event marker. No
  implementations, no third-party types.
- **`BuildingBlocks.Infrastructure`** — the **Publisher**, the **outbox** (write and
  drain), the dispatch loop, the RabbitMQ/MassTransit transport, and a small
  **projection runner** that invokes in-context handlers. All third-party code
  lives here.
- **Each service** — owns its **read-model schema**, its **projection handlers**,
  and its **queries**. **Read models are not a Building Block**: they are
  domain-shaped and belong to the service. Infrastructure ships only the plumbing.

**Rebuildability:** because read models are derived (ADR-0021), a read database can
be rebuilt by **replaying** events (ES) or **re-running** projections over the
source of truth. Read models are disposable; the write side is authoritative.

## Consequences

- **Easier:** one uniform read-model mechanism for ES and state-stored contexts;
  the outbox already needed for ADR-0004 is reused, so integration events and
  projections share the same reliable path; read models are rebuildable; the domain
  stays untouched (it only raises events).
- **Reliable:** at-least-once delivery closes the crash-window drift that a naive
  in-memory publisher would have across two databases.
- **Harder / accepted trade-offs:** projection handlers **must** be idempotent and
  order-aware (with a stored position marker); reads are eventually consistent, so
  read-your-writes must be handled at the BFF/UI where it matters; the outbox and
  its drain loop are operational components to run and monitor.
- Boundaries are preserved: in-context projections use **domain** events;
  cross-context read data arrives only via **integration** events (ADR-0004); no
  context reads another's database.

## Alternatives considered

- **In-memory publisher (no outbox), update read DB right after commit:** simplest
  and lowest-latency, but with two separate databases it has an unrecoverable
  **crash-window** in which the read model silently drifts. Rejected on reliability;
  the outbox is cheap since ADR-0004 already mandates one.
- **Same-transaction (strongly consistent) read updates:** impossible under
  ADR-0021 — the write and read databases are separate, so no single local
  transaction spans them (2PC/distributed transactions are explicitly out of scope).
  Rejected as infeasible.
- **Marten's built-in async projection daemon for ES contexts:** turnkey for
  event-sourced read models, but it would be a **second, ES-only** mechanism
  alongside the Publisher needed for state-stored contexts and integration events.
  Rejected in favour of a single uniform mechanism; may be revisited per context if a
  concrete driver appears.
- **Read model updated by consuming integration events even in-context:** would work
  but routes purely internal projection traffic through RabbitMQ, adding latency and
  broker load for data that never leaves the context. Rejected — **in-context**
  projections use **domain** events directly; RabbitMQ is reserved for genuine
  **cross-context** communication (ADR-0004).
