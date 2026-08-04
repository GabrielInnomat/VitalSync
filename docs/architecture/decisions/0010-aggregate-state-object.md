# 0010. Aggregate state object

- **Status:** Accepted
- **Date:** 2026-06-25
- **Amended:** 2026-07-22 (`IState` is now the self-referencing `IState<TSelf, TKey>` so `Apply` returns the concrete state without a cast; the state object now applies only to event-modeled aggregates — see [ADR-0012](./0012-optional-event-sourcing-aggregate.md))
- **Amended:** 2026-08-03 (the state object is described by the abstract record `AggregateState<TSelf, TKey>` instead of the `IState` interface, so the base can own the aggregate version through the virtual record copy constructor — see [ADR-0030](./0030-persisted-names-and-aggregate-version.md))

## Context

When an aggregate participates in event sourcing, two methods tend to appear per domain event: one on the public command API (the behavior a command invokes) and one that applies the event to mutate state (used both for new events and for replay during rehydration). For a large aggregate with many event types, this "two methods per event" growth makes the aggregate class large and interleaves two concerns — _deciding what should happen_ and _how state reflects what happened_ — in a single file.

Additionally, the platform originally required a single aggregate model for both event-sourced and state-stored persistence (see [ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)). That requirement was later revised: event sourcing is now an opt-in domain-modeling choice and the state object applies only to event-modeled aggregates (see [ADR-0012](./0012-optional-event-sourcing-aggregate.md)).

## Decision

Introduce a dedicated **state object** per event-sourced aggregate, described by `IState<TSelf, TKey>`:

```csharp
public interface IState<TSelf, out TKey>
    where TSelf : IState<TSelf, TKey>
    where TKey : struct, IEntityKey
{
    TKey Id { get; }
    TSelf Apply(IDomainEvent domainEvent);
}
```

- **All apply/evolution logic lives on the state.** The aggregate (`EventSourcedAggregateRoot<TKey, TState>`) exposes only the public command API and delegates state changes to the state's `Apply`.
- **The state owns the aggregate's identity** (`Id`). The aggregate derives its `Id` from the state.
- **State objects are immutable**: `Apply` returns the next state (`this with { … }`). The self-referencing generic guarantees `Apply` returns the concrete `TState`, removing the previous cast.
- `EventSourcedAggregateRoot<TKey, TState>` calls `Apply` in `RaiseEvent` (new events) and `LoadFromHistory` (replay), advances `Version`, and validates identity.
- Apply logic is **on the state itself**, not in a separately injected applier.

## Consequences

- Large event-sourced aggregates stay readable: the aggregate file contains business behavior; the state file contains evolution logic.
- The state object is a small, individually testable unit (state + event → next state).
- `Apply` is statically typed to the concrete state, so call sites need no cast.
- Aggregate identity is established by the first applied event and validated after replay (see [ADR-0008](./0008-entity-identity-and-equality.md)).
- Reading aggregate state at command-time goes through `State.X` (e.g. invariant checks read from `State`), a minor ergonomic shift.
- State-modeled aggregates (`AggregateRoot<TKey>`) do not use a state object at all; this pattern is scoped to event-modeled aggregates (ADR-0012).

> **Implementation note (amendment 2026-07-23):** With the re-unified hierarchy of [ADR-0025](./0025-unified-state-fold-aggregate-model.md), the state-object pattern described here applies to **every** aggregate: `AggregateRoot<TKey, TState>` folds events into the state, and the event-sourced base only adds `Version`/`LoadFromHistory`. The last consequence bullet ("state-modeled aggregates do not use a state object") is obsolete.

## Alternatives considered

- **Apply-on-the-aggregate** (mutating methods directly on the aggregate): simplest, but produces the "two methods per event" growth and interleaves concerns in large aggregates.
- **Injected `IEventApplier<TState>`** (apply logic in a separate injected service): adds constructor ceremony to every aggregate and splits a state and its evolution across two types; rejected in favor of apply-on-the-state.
- **Non-self-referencing `IState<TKey>` returning `IState<TKey>`:** required an unchecked cast back to the concrete state in the aggregate; replaced by the self-referencing form.

## Amendment, 2026-08-04: the pattern extends to child entities

[ADR-0032](./0032-child-entities-raise-via-root.md) introduces `EntityState<TSelf, TKey>` — the same self-referencing, immutable, pure-`Apply` shape for a child entity inside an aggregate, minus the version, which stays on the aggregate ([ADR-0030](./0030-persisted-names-and-aggregate-version.md)). The root state folds its children (usually by delegating to their `Apply`), so there is still exactly **one** fold path and `Apply` remains free of side effects: a state never records an event, not even for a child.
