# 0011. Unified aggregate for event sourcing and EF Core

- **Status:** Accepted
- **Date:** 2026-06-25
- **Amended:** 2026-07-21 (event-sourcing capability is now exposed via *explicit* interface implementation, not on the public aggregate surface; added a replay-misuse guard)

## Context

The architecture states that Event Sourcing is used *where it provides business value* and traditional persistence with EF Core *in all other cases*, and the decision for any given Bounded Context is an infrastructure concern — not a domain concern.

A naive approach provides two different aggregate base classes — one for event sourcing and one for state-stored persistence — forcing the domain author to **choose a base class based on the persistence strategy**. This leaks the infrastructure decision into the domain and makes switching a context between EF Core and event sourcing a costly rewrite of its aggregates.

The two worlds genuinely differ in mechanics: event-sourced aggregates change state by applying events (and need replay and a version), while state-stored aggregates load and mutate current state. A unification must reconcile these without imposing one world's ceremony on the other, and — importantly — **without leaking event-sourcing members onto the public surface of a state-stored aggregate**.

## Decision

Provide a **single aggregate base**, `AggregateRoot<TKey, TState>`, used by both worlds. The reconciliation is achieved via the state object (see [ADR-0010](./0010-aggregate-state-object.md)): in both worlds an aggregate's current condition is a `TState`.

- State changes go through `RaiseEvent` (apply via the state, validate identity, advance the internal version, record the event).
- The base class implements `IEventSourcedAggregateRoot<TKey>` (`Version` + `LoadFromHistory`) **explicitly**, mirroring how `IDomainEventsManager.ClearDomainEvents()` is implemented explicitly (see [ADR-0007](./0007-read-only-vs-managed-domain-events.md)). As a result, `Version` and `LoadFromHistory` are **not** on a concrete aggregate's public surface; a caller must deliberately obtain the `IEventSourcedAggregateRoot<TKey>` view to reach them:

  ```csharp
  long IEventSourcedAggregateRoot<TKey>.Version => _version;
  void IEventSourcedAggregateRoot<TKey>.LoadFromHistory(IEnumerable<IDomainEvent> history) => /* replay */;
  ```

- **Event-sourced** services persist the raised events and rebuild via replay through the `IEventSourcedAggregateRoot<TKey>` view.
- **State-stored (EF Core)** services persist the `TState` directly; they never cast to the ES view, so `Version` and `LoadFromHistory` are invisible to them and `Apply` is inert.
- **Replay-misuse guard:** `LoadFromHistory` throws a `DomainValidationException` if it is called on an aggregate that already has state (internal version `> 0`). This turns accidental rehydration of an already-materialized aggregate into an immediate, obvious failure.

Because there is a single base class, the domain author never chooses a base class by persistence strategy. **The persistence choice is made in the Application/Persistence (composition) layer** — by selecting an event-sourced or an EF-Core repository for the context — not by the aggregate's type. The aggregate stays oblivious to how it is stored.

## Consequences

- The persistence strategy does not leak into the domain: aggregates carry no persistence-specific base class, and event-sourcing members are not on their public surface.
- Switching a context between EF Core and event sourcing does not require rebasing its aggregates onto a different class hierarchy — only the repository chosen in the composition layer changes.
- A single, consistent aggregate model is taught, tested, and tooled across all services.
- The same `TState` is persisted by EF Core and rebuilt by replay in event sourcing.
- Event-sourcing capability is present on every aggregate but reachable **only** via an explicit `IEventSourcedAggregateRoot<TKey>` cast, so there is **zero** ceremony on the surface a domain author or an application handler sees — only infrastructure that deliberately opts in observes `Version`/`LoadFromHistory`.
- The replay-misuse guard prevents an EF-Core (or any) repository from silently corrupting an already-loaded aggregate by replaying history over it.

## Alternatives considered

- **Two base classes (separate ES and EF aggregates):** cleanest per-paradigm separation, but forces the domain author to choose a base class by persistence strategy and makes EF↔ES migration a rewrite. Rejected — it reintroduces the infrastructure-in-domain coupling we set out to remove, and it means the persistence decision can no longer be made purely in the composition layer.
- **One base, ES members public (previous implementation):** simplest, but leaks `Version`/`LoadFromHistory` onto every aggregate's public surface, including pure state-stored ones. Rejected — the explicit-interface approach keeps the single base class while removing the leak.
- **One base, apply-always (event-apply even for CRUD):** uniform, but imposes event-sourcing ceremony on simple state-stored aggregates that gain nothing from it. Rejected as a ceremony tax on the EF-Core majority.
- **Unify only the contracts (shared interfaces), keep separate bases:** keeps the authoring-time split (still two base classes); does not meet the goal of a single aggregate model.
