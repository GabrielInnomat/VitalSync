# 0022. Event-driven read models via an outbox-backed publisher

- **Status:** Accepted
- **Date:** 2026-07-26
- **Amended:** 2026-07-31 (integration-event publication is bound to the handler's message context — see the note below)
- **Amended:** 2026-08-10 (projection and integration-event publication are separate queues, and a lost projection is visible — see the amendment at the end)

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

> **Delivery does not end at the handover (2026-08-04).** The outbox guarantee above
> covers the write transaction and the flush; it originally stopped there, because the
> RabbitMQ sending endpoint was buffered — no persisted outgoing envelope, and a
> non-persistent AMQP message. Both are fixed in the persistent-delivery amendment to
> [ADR-0023](./0023-wolverine-messaging-transport.md), which is what makes the
> at-least-once promise hold across a broker or process restart.

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

## Amendment (2026-08-07) — rebuildability holds on both paths, by different means

The rebuildability promise above was only true for the event-sourced path. In a state-stored
context the outbox row is deleted once delivered, so after the flush no replayable record of a
domain event survives and a buggy projection produced a read model nobody could repair.

ADR-0036 closes that gap **without** introducing a replay: the live path stays event-based and
unchanged, and a second, explicitly invoked path (`IReadModelRebuilder` plus
`ReadModelRebuildRunner`) derives the read model from the **current aggregate state**. The
consequence for read-model design is a hard rule: **every field of a state-stored read model must be
a function of the current aggregate state**. A field that needs history belongs in an event-sourced
context.

## Amendment (2026-08-10) - the two consumers are split, and giving up on one is visible

Both concerns above - updating the in-context read model and publishing integration events - ran
in **one** Wolverine handler over one envelope. That coupled two things the business treats
differently. A projection handler that threw took the whole envelope down, so the integration
event was never published either; downstream contexts stayed unaware of a fact that had already
been committed. The two failures are not equally bad: a read model is derived and rebuildable
(ADR-0036), an integration event that was never sent is not recoverable from anywhere.

`DomainEventEnvelope` therefore now carries **only** the integration-event step. Its handler
publishes, and then forwards a `ProjectionEnvelope` to a second local queue whose handler runs
the projections. Both queues carry `UseDurableInbox()` and are ordered per aggregate, so per-aggregate
order and crash safety are unchanged, and both still start from the same outbox flush - neither
runs unless the write transaction committed. The ordering is deliberate: the non-recoverable
step goes first.

Three commitments follow, and each has a test in `DispatchIsolationTests`:

1. A command that does not commit produces **neither** a projection **nor** an integration event.
2. A failing projection does **not** stop the integration event.
3. A failing integration-event mapper **does** stop the projection, because it fails before the
   forward.

### The cost this amendment pays for

Splitting the queues makes a projection failure **quiet**. Before, it was loud by accident: nothing
was published, so the gap surfaced downstream. Now the host keeps working and only a dead letter
records the loss - and the loss is real, because the read model silently misses that change until
someone rebuilds it.

Where that dead letter lands is not where it lands for a consumer. An integration event arrives
over a RabbitMQ listener, so Wolverine moves it to `wolverine-dead-letter-queue` on the broker. A
projection envelope travels on a **local** queue, which has no broker endpoint, so it is written to
the `wolverine_dead_letters` table in the write database. That table was previously empty by
design; it no longer is, and it is the only place a lost projection is recorded.

`AddBuildingBlocks` therefore registers the health check `building-blocks-dead-letters` (tag
`dead-letters`) whenever a persistence strategy was selected. It counts that table, capped at
1000 rows so the query cannot become expensive, and reports **`Degraded`** with the count.

`Degraded` rather than `Unhealthy` is the load-bearing choice. `Unhealthy` maps to HTTP 503,
which would take the host out of readiness: Aspire would restart or drain it and the BFF would stop
routing to it, turning "a read model is stale" into an outage - while restarting fixes nothing,
because the dead letter is durable and the row is still there afterwards. `Degraded` maps to 200,
so the host keeps serving while the condition is plainly visible in `/health` and on the Aspire
dashboard. A **missing** table also reports `Degraded`: a check that cannot see failures must not
report health.

### Rejected alternatives

- **Log the dead letter and rely on log alerts.** A log line is an event, and this is a *state* -
  the read model stays wrong until someone acts. A health check keeps saying so until the table is
  empty, and it needs no external alerting rule to be configured correctly.
- **Retry the projection forever instead of dead-lettering.** The failure this protects against is
  a projection **bug**, which no retry ladder fixes; an unbounded retry would spin on every
  redelivery and bury the broker in noise while the read model stays wrong anyway.
- **Keep one queue and publish the integration event even when the projection throws.** That is the
  same coupling with an exception carved into it, and it would still lose the projection silently
  while making the handler's contract depend on the order of two unrelated steps.

## Amendment (2026-08-11) - the queues are partitioned per aggregate, not serialised globally

Both local queues used `.Sequential()`. That bought a **per-aggregate** ordering guarantee by
serialising **every** domain event of the whole service: throughput was capped at one event at a
time, for a promise that only ever concerned one aggregate at a time.

Both queues now use `PartitionProcessingByGroupId(PartitionSlots.Five)`, with the group id supplied
by `options.MessagePartitioning.ByMessage<...>` as `"{AggregateName}/{AggregateId}"` - both fields
travel on the envelope since ADR-0030. Wolverine keeps messages of one group id off each other
while letting different group ids run at the same time, so the guarantee is unchanged and the
global cap is gone.

Three properties were verified against a real message store rather than assumed
(`DomainEventPartitioningBehaviourTests`):

1. **Within one group, a message in a retry cooldown is not overtaken.** The slot blocks for the
   cooldown instead of moving on. This is what makes the ordering guarantee survive the retry
   ladder, and it was measured, not read from the documentation - had the queue moved on, a later
   event would have advanced the read-model watermark past the waiting one and the retry would have
   been discarded on success.
2. **Across groups, messages are handled concurrently.**
3. **The wiring itself is partitioned.** Asserted on a started host, because Wolverine applies
   listener configuration during bootstrapping - at configuration time `GroupShardingSlotNumber` is
   still `null`, so a check against `WolverineOptions` alone would pass no matter what was
   configured.

### The slot count is a constant, not a setting

`PartitionSlots.Five` is fixed in `WolverineOptionsExtensions`. Changing it remaps aggregates onto
slots, so two processes running different slot counts can handle the same aggregate at the same
time - a rolling restart would then reorder events and the watermark would drop the loser silently.
Changing this number requires a drain, which is precisely the operation a configuration knob invites
an operator to skip.

### What this does not fix

The guarantee is per **process**, exactly as it was with `.Sequential()`: a local queue lives in one
host. With more than one instance of a bounded context, two hosts process their own commits
concurrently and per-aggregate order across them is not guaranteed. That gap is pre-existing and
orthogonal - partitioning neither widens nor closes it.

It is also unreachable today, and that was measured rather than assumed: property 1 above shows a
retry cooldown blocks its slot, so within one process no event ever passes a waiting predecessor.
Every path that could drop an event silently therefore needs a second host.

**Running more than one instance of a bounded context requires a gap detection first.** This is a
named precondition of scaling out, not an open improvement, because the read-model watermark
*discards* an out-of-order event rather than merging it (ADR-0030) and the sample projections write
increments - a discarded event is permanently wrong, not briefly stale. Three things are already
known about that work, so the decision does not start from scratch:

- The detection belongs in the **dispatch path** (`ProjectionRunner`), never in the read-model
  watermark. A domain event with no projection handler is legitimate and produces a perfectly
  healthy version gap; a check on the watermark would raise false alarms on it.
- It needs a **per-aggregate progress record**, and where that lives is the open design question:
  Building Blocks deliberately has no access to the read database, and the ADR-0036 rebuild would
  have to reset it along with the read model.
- Visibility follows `DeadLetterHealthCheck`: `Degraded`, never `Unhealthy` - a stale read model is
  not an outage - with the existing rebuild as the repair.
