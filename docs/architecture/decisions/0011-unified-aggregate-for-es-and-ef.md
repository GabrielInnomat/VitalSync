# 0011. Unified aggregate for event sourcing and EF Core

- **Status:** Accepted
- **Date:** 2026-06-25

## Context

The architecture states that Event Sourcing is used *where it provides business value* and traditional persistence with EF Core *in all other cases*, and the decision for any given Bounded Context may be made (or changed) later. The persistence strategy is an **infrastructure** concern, and the domain must remain independent of infrastructure.

A naive approach provides two different aggregate base classes — one for event sourcing and one for state-stored persistence — forcing the domain author to **choose a base class based on the persistence strategy**. This leaks the infrastructure decision into the domain and makes switching a context between EF Core and event sourcing a costly rewrite of its aggregates.

The two worlds genuinely differ in mechanics: event-sourced aggregates change state by applying events (and need replay and a version), while state-stored aggregates load and mutate current state. A unification must reconcile these without imposing one world's ceremony on the other unnecessarily.

## Decision

Provide a **single aggregate base**, `AggregateRoot<TKey, TState>`, used by both worlds. The reconciliation is achieved via the state object (see [ADR-0010](./0010-aggregate-state-object.md)): in both worlds an aggregate's current condition is a `TState`.

- State changes go through `RaiseEvent` (apply via the state, advance `Version`, record the event).
- `LoadFromHistory` rebuilds state by replay; it is used by event sourcing and simply not called by state-stored services.
- `Version` is meaningful for event sourcing and unused for state-stored services.
- **Event-sourced** services persist the raised events and rebuild via replay.
- **State-stored (EF Core)** services persist the `TState` directly; `Apply`, `Version`, and `LoadFromHistory` are inert for them.
- An optional marker interface, `IEventSourcedAggregateRoot<TKey>`, exposes `Version` and `LoadFromHistory` so that event-sourcing infrastructure can constrain to it (`where T : IEventSourcedAggregateRoot<TKey>`). An aggregate opts in by declaring the marker; state-stored aggregates use the same base without it.

The aggregate author therefore picks a *style* (raise+apply for everything, or persist state), **not a base class**, and the persistence decision does not change the aggregate's type hierarchy.

## Consequences

- The persistence strategy no longer leaks into the choice of aggregate base class.
- Switching a context between EF Core and event sourcing does not require rebasing its aggregates onto a different class hierarchy.
- A single, consistent aggregate model is taught, tested, and tooled across all services.
- The same `TState` is persisted by EF Core and rebuilt by replay in event sourcing.
- State-stored aggregates carry some members (`Version`, `LoadFromHistory`) that are inert for them — accepted as mild conceptual noise in exchange for one unified model.
- Event-sourcing infrastructure can still require ES-specific capabilities via the optional `IEventSourcedAggregateRoot<TKey>` marker.

## Alternatives considered

- **Two base classes (separate ES and EF aggregates):** cleanest per-paradigm separation, but forces the domain author to choose a base class by persistence strategy and makes EF↔ES migration a rewrite. Rejected — it reintroduces the infrastructure-in-domain coupling we set out to remove.
- **One base, apply-always (event-apply even for CRUD):** uniform, but imposes event-sourcing ceremony on simple state-stored aggregates that gain nothing from it. Rejected as a ceremony tax on the EF-Core majority.
- **Unify only the contracts (shared interfaces), keep separate bases:** keeps the authoring-time split (still two base classes); does not meet the goal of a single aggregate model.