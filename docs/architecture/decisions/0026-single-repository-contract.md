# 0026. Single repository contract: add and get, no delete

- **Status:** Accepted
- **Date:** 2026-07-30

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
