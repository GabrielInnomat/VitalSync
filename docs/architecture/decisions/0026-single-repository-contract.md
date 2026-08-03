# 0026. Single repository contract: add and get, no delete

- **Status:** Accepted
- **Date:** 2026-07-30
- **Amended:** 2026-08-02 (EF Core tracks the state, not the aggregate — see the note below)
- **Amended:** 2026-08-03 (the contract constrains reconstitution; the rehydration-constructor question is answered)

## Context

The Application layer exposed two repository contracts: `IRepository<TAggregate, TKey>` for state-stored aggregates
(`GetByIdAsync`, `AddAsync`, `Remove`) and `IEventSourcedRepository<TAggregate, TKey>` for event-sourced aggregates
(`GetByIdAsync`, `SaveAsync`). Handlers therefore depended on the persistence style of their aggregate — the exact
coupling the unified aggregate model ([ADR-0025](./0025-unified-state-fold-aggregate-model.md)) removes from the
domain would have survived in the application layer, and switching a context to event sourcing would still have
rewritten every handler.

Two observations make a merge possible:

1. **VitalSync never hard-deletes data.** Removal is a business state change (a *soft delete*) expressed by the
   aggregate raising an event — persistence-wise it is an ordinary update. A `Remove` method on the repository is
   therefore not just unnecessary; it would invite bypassing the domain model.
2. **Both persistence styles can track.** EF Core change-tracks retrieved aggregates, so updates need no repository
   call — the `IUnitOfWork` commit persists them. The Marten repository can mirror this: it registers every
   aggregate it hands out (loaded or added) with the scoped `MartenAggregateTracker`, and the `MartenUnitOfWork`
   appends the tracked aggregates' uncommitted events (expected-version optimistic concurrency) when it commits. An
   explicit `SaveAsync` becomes unnecessary.
   _"EF Core change-tracks retrieved aggregates" is superseded by the tracking amendment below; the conclusion —
   no `SaveAsync` — is not._

> **Tracking (amendment 2026-08-02).** Observation 2 reaches the right conclusion through a mechanism that no longer
> exists. EF Core does **not** change-track the retrieved aggregate: since the state-mapping amendment of
> [ADR-0025](./0025-unified-state-fold-aggregate-model.md) the mapped entity type is the aggregate's **state**, so the
> change tracker only ever sees states and cannot answer "which aggregates took part in this command".
>
> The EF Core path therefore does exactly what this ADR already described for Marten:
>
> - **`EfCoreRepository` registers every aggregate it hands out** — loaded or added — with the scoped
>   **`EfCoreAggregateTracker`**, together with the state instance EF Core tracks for it. Tracking is idempotent per
>   aggregate instance.
> - **`EfCoreUnitOfWork` walks those entries at commit**: it copies each aggregate's current state onto its tracked
>   entity (`CurrentValues.SetValues`), enrolls the uncommitted domain events in the outbox, saves, and clears the
>   events afterwards. The copy is load-bearing rather than defensive — state objects are immutable, so every applied
>   event replaced the instance and left the tracked one stale; without it the change tracker would find nothing to
>   save and a rename would be silently lost.
>
> Both persistence styles are thereby symmetric for the first time: repository tracks, unit of work writes.
>
> **The decision is unchanged.** `GetByIdAsync` still "retrieves **and tracks**", there is still no `Update`/`Save`
> and no `Remove`, and handlers stay persistence-agnostic. The consequence below about the uniform tracking model —
> the unit of work, not the repository, performs the write — now describes **both** sides instead of only Marten's.
>
> **A behavioral consequence worth knowing.** Domain events are collected **only from aggregates that passed through
> `IRepository`**. Writing an entity straight into the `DbContext` produces no events at all, because nothing
> registers it with the tracker. That is consistent with this ADR — the repository is the way in — but it is a real
> break: the pre-existing EF Core outbox test wrote a plain entity, silently stopped producing an outbox entry, ran
> into a timeout, and was moved onto a real aggregate. Pinned by `EfCoreOutboxAtomicityTests` (exactly one SQL command
> touching aggregate table and outbox) and `EfCoreAggregateRoundTripTests`.
>
> **Rehydration constructor.** Because the repository builds an empty aggregate and restores the loaded state into it,
> a state-stored aggregate needs a **parameterless constructor**. `EfCoreRepository` uses
> `Activator.CreateInstance(…, nonPublic: true)`, so a non-public one suffices; the event-sourced repository's `new()`
> constraint requires a **public** one. Whether `IRepository` should impose one uniform rule instead of two implicit
> ones is open — see `WalkingSkeleton.md` §9.
> _Superseded by the reconstitution amendment below; the open question it names is answered there._

> **Reconstitution (amendment 2026-08-03).** The open question above is settled: `IRepository<TAggregate, TKey>`
> **does** impose one uniform rule, and it is neither of the two implicit ones. `TAggregate` is now additionally
> constrained to **`IReconstitutable<TAggregate>`** (`static abstract TSelf CreateEmpty()`), which both
> implementations call — no `Activator`, no `new()`, no public constructor, and no asymmetry between the persistence
> paths. Because the constraint sits on the contract, a wrongly shaped aggregate is a compile error at the injection
> site rather than a container failure on first use. The rationale and the residual hole are recorded in the
> reconstitution amendment of [ADR-0025](./0025-unified-state-fold-aggregate-model.md).
>
> The two members and their semantics are untouched: `GetByIdAsync` still retrieves **and tracks**, `AddAsync` still
> registers, and there is still no `Remove`/`Update`/`Save`.
>
> **Tracker signature, while nearby.** `EfCoreAggregateTracker.Track` now receives the `IStateOwner` from the
> repository, which had already resolved it to find the state type, instead of re-deriving it and throwing an
> `ArgumentException` when the cast failed. One fact, established once, at the only place that can establish it —
> the failure mode is removed rather than reported. Pinned by `EfCoreAggregateTrackerTests`.

## Decision

Merge the two contracts into a single `IRepository<TAggregate, TKey>` (constrained to `IAggregateRoot<TKey>`) with
exactly two members:

- `Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken)` — retrieves **and tracks** the
  aggregate; subsequent changes flow through the `IUnitOfWork` at commit with no further repository call.
- `Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken)` — registers a newly created aggregate;
  its state (EF Core) or its uncommitted events (event store) become durable when the unit of work commits.

There is deliberately **no `Remove`** (soft delete is an update), **no `Update`/`Save`** (tracking + unit of work),
and no query surface beyond `GetByIdAsync` (queries read the context's read database directly, ADR-0021/0022).

`IEventSourcedRepository` is deleted. Both `UseEfCorePersistence` and `UseMartenEventSourcing` register their
implementation under the same open-generic `IRepository<,>` contract; a host selects exactly one persistence style
per write database.

## Consequences

- Handlers are persistence-agnostic: the same load → mutate → (commit) and create → `AddAsync` → (commit) flow works
  for state-stored and event-sourced aggregates alike, completing the cheap-switch goal of ADR-0025.
- Hard deletion is impossible through the repository; deleting data now *requires* modeling it in the domain, which
  is the intended policy.
- The uniform tracking model means the Marten unit of work — not the repository — performs the stream append; the
  repository stages nothing in the session.
- If a genuine hard-delete requirement ever appears (e.g. GDPR erasure), it will need a deliberate, separate
  mechanism — that friction is intentional.

## Alternatives considered

- **Keep two contracts:** rejected — it re-couples handlers to the persistence style and makes the ES switch a
  handler rewrite.
- **Single contract with `Remove`:** rejected — VitalSync soft-deletes; a hard-delete API would be dead code at best
  and a domain-model bypass at worst.
- **Single contract with an explicit `SaveAsync`/`Update`:** rejected — EF Core does not need it, and requiring it
  only for ES would leak the persistence style back into handlers; tracking in the Marten repository removes the
  need.
