# 0025. Unified state-fold aggregate model with additive event sourcing

- **Status:** Accepted
- **Date:** 2026-07-30
- **Supersedes:** [ADR-0012](./0012-optional-event-sourcing-aggregate.md)

## Context

[ADR-0012](./0012-optional-event-sourcing-aggregate.md) split the aggregate hierarchy into a state-modeled
`AggregateRoot<TKey>` (free properties, `AddDomainEvent`) and an event-modeled
`EventSourcedAggregateRoot<TKey, TState>` (state fold via `RaiseEvent`, `Version`, `LoadFromHistory`). Apart from
`IAggregateRoot<TKey>` and the equality logic the two bases shared **nothing**: identity acquisition, state
representation, the event-raising API, versioning, construction, and the repository contract all differed.

That made the declared strategy — "event sourcing selectively, where it carries business value" — structurally
unsupported. The value of event sourcing typically becomes apparent only after a business need for history emerges,
so a framework backing that strategy must make the *later* switch cheap. Under the split hierarchy the switch was a
re-implementation: new base class, new state type, rewritten methods, a different repository interface, different
tests. Two programming models also meant double onboarding, double conventions, and double review rules
(IMP-10 in `Improvements.md`).

A further irritation: `Entity<TKey>`, `AggregateRoot<TKey>`, and `EventSourcedAggregateRoot<TKey, TState>` carried
three identical copies of the identity-equality implementation.

## Decision

Provide a **single authoring model** in which the persistence strategy is a configuration and repository decision,
not a class-hierarchy decision. The state fold is used by **all** aggregates:

- **`AggregateRoot<TKey, TState>`** — the one base for every aggregate. It holds the immutable
  `protected TState State` ([ADR-0010](./0010-aggregate-state-object.md)), derives `Id` from `State.Id`, and exposes
  a single mutation path: `RaiseEvent(IDomainEvent)` folds the event into the state
  (`State = State.Apply(event)`), validates the identity, and records the event for dispatch. The state fold is
  valuable even without event sourcing: it forces every state change to be expressed as an event instead of slipping
  past via a property setter — which is exactly why aggregates produce events in the first place.
- **`EventSourcedAggregateRoot<TKey, TState>`** — derives from `AggregateRoot<TKey, TState>` and adds **only** the
  event-sourcing capability: a `Version` for optimistic concurrency and `LoadFromHistory` for replay, both still
  explicitly implemented behind the `IEventSourcedAggregateRoot<TKey>` view ([ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)
  amendment, [ADR-0019](./0019-event-store-technology-marten.md)).
- **`EntityBase<TKey>`** — a new common base holding the single identity-equality implementation
  ([ADR-0008](./0008-entity-identity-and-equality.md)); `Entity<TKey>` (eager, constructor-validated identity) and
  `AggregateRoot<TKey, TState>` (state-derived, per-transition-validated identity) both derive from it. Its
  constructor is `private protected`, so domain code derives from those two, never from it directly.

Switching an aggregate between the state-stored and event-sourced worlds is now **additive**: change the base class
(the business code, state type, and tests stay untouched) and change the repository registration in the composition
layer (see [ADR-0026](./0026-single-repository-contract.md), which merges the repository contracts so even the
handler code is unaffected).

Differences from the *first* unification attempt ([ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)): the
event-sourcing members (`Version`, `LoadFromHistory`) are **not** present on every aggregate — they live only on the
derived event-sourced base — so a state-stored aggregate carries no replay surface at all, while the authoring model
(state fold) is still shared.

**Trade-off, accepted knowingly:** for simple CRUD-like aggregates the fold is ceremony, and EF Core must map the
`State` record (via `ComplexProperty`/owned-entity or explicit column mapping) instead of plain auto-properties.
This price is paid for a consistent single model and a cheap ES migration path; the previous state — an advertised
strategy without structural support — was judged worse.

## Consequences

- One programming model to learn, review, and tool; the "which base class?" question is now purely "does the event
  history carry business value?".
- The state-stored → event-sourced switch is a base-class change plus a composition-layer change; domain logic,
  state types, handlers, and tests survive unchanged.
- The timestamp/stamping path and the raising API are identical for all aggregates; divergence bugs between the two
  models can no longer occur.
- Identity equality is implemented exactly once (`EntityBase<TKey>`).
- State-stored aggregates gain the immutable state fold: better testability, no mutation outside events.
- EF Core mapping targets the state record, which costs more configuration than mapping free properties.
- `AddDomainEvent` no longer exists; `RaiseEvent` is the single raising API.

## Alternatives considered

- **Keep the split hierarchy (ADR-0012):** rejected — it makes the selective-ES strategy fictional, because the
  switch is maximally expensive precisely when its need is discovered.
- **Declare the ES↔state-stored switch an unsupported scenario:** honest, but rejected — history-driven requirements
  are expected in the health/fitness domain, so the migration path carries real value.
- **Return to the single base with ES members on every aggregate (ADR-0011):** rejected — it leaks
  `Version`/`LoadFromHistory` onto aggregates that never replay; the derived-class split keeps ES strictly additive.
