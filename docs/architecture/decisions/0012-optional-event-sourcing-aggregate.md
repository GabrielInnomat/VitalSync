# 0012. Optional event sourcing via a split aggregate hierarchy

- **Status:** Accepted
- **Date:** 2026-07-22
- **Supersedes:** [ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)

## Context

[ADR-0011](./0011-unified-aggregate-for-es-and-ef.md) provided a single `AggregateRoot<TKey, TState>` base for both event-sourced and state-stored aggregates, reconciled through a state object ([ADR-0010](./0010-aggregate-state-object.md)). The goal was to keep the persistence choice out of the domain.

In practice the unified base did not achieve that goal cleanly. It **forced every aggregate to be event-sourced in its mechanics**: the only way to change state was `RaiseEvent -> State.Apply(event)`, even for aggregates whose event history carries no business value. That imposed event-sourcing ceremony (a `TState`, an `Apply` switch, synthetic events for trivial changes) on aggregates that only needed to mutate current state, and it leaked a storage-shaped vocabulary into the domain.

Two decisions were tangled together in 0011:

- **Decision A — "Is this aggregate modeled with events?"** This is a *domain-modeling* decision, made when the sequence of events is itself business value (audit, temporal reasoning, rich history).
- **Decision B — "Is this aggregate physically stored as an event stream or a current-state row?"** This is an *infrastructure* concern and must not appear in the domain.

0011 collapsed both into one base class, which forced Decision A's mechanics onto everyone in the name of keeping Decision B out of the domain.

## Decision

Separate the two decisions and make event sourcing an **opt-in domain-modeling choice** via two distinct base classes:

- **`AggregateRoot<TKey>`** — state-modeled. The aggregate mutates its own fields directly and records domain events via `AddDomainEvent`. No `State`, `Apply`, `Version`, or `RaiseEvent`. This is the default for aggregates whose history is not itself business value.
- **`EventSourcedAggregateRoot<TKey, TState>`** — event-modeled. Chosen when event history carries business value. Carries all event-sourcing machinery (`RaiseEvent`, `State.Apply`, `Version`, `LoadFromHistory`).

Both implement the common `IAggregateRoot<TKey>` marker (identity + domain events). Selecting a base is a statement about **modeling**, never about storage.

Supporting changes:

- **Persistence stays out of the domain.** No domain type references EF Core, databases, or event stores. The Persistence/Application layer selects physical storage by detecting `IEventSourcedAggregateRoot<TKey>`.
- **Business rules are checked before events are raised.** The aggregate's command method validates invariants (via `RuleChecker.Check(...)`) *before* it calls `RaiseEvent`/`AddDomainEvent`, so a rule violation never records an event or mutates the aggregate. Within `RaiseEvent` the event is applied to the state first and the **identity** guard runs immediately after — this ordering is required because the identity only comes into existence once the creation event has been applied, so it cannot be checked earlier.
- **Type-safe state.** `IState<TSelf, TKey>` (self-referencing, see amended [ADR-0010](./0010-aggregate-state-object.md)) makes `Apply` return the concrete state, removing the previous cast.
- **Snapshot-friendly rehydration.** `LoadFromHistory` does not forbid replay based on version; it guards only against replaying onto an aggregate that already has **uncommitted events** (throws `InvalidOperationException`). This keeps snapshot-based rehydration (restore state + version, then replay only the events after the snapshot) possible.
- **Events are pure data.** `DomainEvent` no longer takes an `IClock`; `OccurredAt` is stamped at raise time by the aggregate.
- **Key abstraction is not locked to `Guid`.** `IEntityKey` is a marker (also declaring `IsEmpty`) with `IEntityKey<TValue>` exposing the underlying value.

## Consequences

- Event sourcing is genuinely *selective*: an aggregate is event-sourced only when a developer deliberately chooses `EventSourcedAggregateRoot`, because the history is business value.
- State-modeled aggregates carry zero event-sourcing ceremony — no `TState`, no `Apply` switch, no synthetic events for simple changes.
- The domain is persistence-ignorant; the storage decision lives entirely in infrastructure, which detects `IEventSourcedAggregateRoot`.
- Choosing a base is now a modeling decision made by the domain author, and it does **not** determine or reveal storage.
- Two base classes must be taught and tooled instead of one, and migrating an aggregate between the state-modeled and event-modeled worlds means changing its base class (an intentional, explicit act).
- Because the package is not yet consumed by any service, this restructure has zero blast radius.

## Alternatives considered

- **Keep the unified base (ADR-0011):** rejected — it forces event-sourcing mechanics onto every aggregate and leaks a storage vocabulary into the domain, defeating selective event sourcing.
- **One base with opt-in `TState`/`Apply` machinery:** less discoverable; the presence or absence of event modeling becomes a runtime convention rather than a clear type choice.
- **Drive the split by storage (an "EF aggregate" vs an "ES aggregate"):** rejected — that is Decision B and must not live in the domain; it would re-introduce the exact leak this ADR removes.
