# 0012. Optional event sourcing via a split aggregate hierarchy

- **Status:** Accepted
- **Date:** 2026-07-22
- **Supersedes:** [ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)

## Context

[ADR-0011](./0011-unified-aggregate-for-es-and-ef.md) provided a single `AggregateRoot<TKey, TState>` base for both event-sourced and state-stored aggregates, reconciled through a state object ([ADR-0010](./0010-aggregate-state-object.md)). The intent was to keep the persistence strategy out of the domain so the choice could be made in the composition layer.

In practice the unified base did not achieve that goal cleanly. It **forced every aggregate to be event-sourced in its mechanics**: the only way to change state was `RaiseEvent -> State.Apply(event) -> new State`, and every aggregate had to supply a `TState` with an `Apply` switch. State-stored aggregates that wanted to simply set a field had to invent a domain event, add an `Apply` case, and route through the event-sourcing pipeline. That is a ceremony tax on aggregates that gain nothing from event sourcing, and it contradicts the platform's stated goal of *selective* event sourcing (used only where event history is business value). The base class even reasoned about "EF Core" — a storage concern leaking into the domain.

Two decisions were tangled together in 0011:

- **Decision A — "Is this aggregate modeled with events?"** This is a *domain-modeling* decision, made when the sequence of events is itself business value (audit, temporal reasoning, rich history). It legitimately belongs in the domain.
- **Decision B — "Is this aggregate physically stored as an event stream or a current-state row?"** This is an *infrastructure* concern and must not appear in the domain.

0011 collapsed both into one base class, which forced Decision A's mechanics onto everyone in the name of keeping Decision B out of the domain.

## Decision

Separate the two decisions and make event sourcing an **opt-in domain-modeling choice** via two distinct base classes:

- **`AggregateRoot<TKey>`** — state-modeled. The aggregate mutates its own fields directly and records domain events via `AddDomainEvent`. No `State`, `Apply`, `Version`, or `RaiseEvent`. This is the default; the aggregate's meaning is its current state.
- **`EventSourcedAggregateRoot<TKey, TState>`** — event-modeled. Chosen when event history carries business value. Carries all event-sourcing machinery (`RaiseEvent`, `State.Apply`, `Version`, `LoadFromHistory`), with the event-sourcing surface exposed only through explicit `IEventSourcedAggregateRoot<TKey>` implementation.

Both implement the common `IAggregateRoot<TKey>` marker (identity + domain events). Selecting a base is a statement about **modeling**, never about storage.

Supporting changes:

- **Persistence stays out of the domain.** No domain type references EF Core, databases, or event stores. The Persistence/Application layer selects physical storage by detecting `IEventSourcedAggregateRoot<TKey>`. An event-modeled aggregate may still be stored state-based (snapshot), and a state-modeled aggregate still emits domain events for integration — Decision A and Decision B remain independent.
- **Invariants are checked before events are applied.** `RaiseEvent` no longer mutates state before validation; a rule violation can never leave a half-mutated aggregate.
- **Type-safe state.** `IState<TSelf, TKey>` (self-referencing, see amended [ADR-0010](./0010-aggregate-state-object.md)) makes `Apply` return the concrete state, removing the previous cast.
- **Snapshot-friendly rehydration.** `LoadFromHistory` no longer forbids replay when a version exists and validates identity once (after the identity-setting event) rather than on every applied event, keeping a future snapshot-then-replay path open.
- **Events are pure data.** `DomainEvent` no longer takes an `IClock`; `OccurredAt` is stamped at raise time by the aggregate.
- **Key abstraction is not locked to `Guid`.** `IEntityKey` is a marker with `IEntityKey<TValue>` exposing the underlying value.

## Consequences

- Event sourcing is genuinely *selective*: an aggregate is event-sourced only when a developer deliberately chooses `EventSourcedAggregateRoot`, because the history is business value.
- State-modeled aggregates carry zero event-sourcing ceremony — no `TState`, no `Apply` switch, no synthetic events for simple changes.
- The domain is persistence-ignorant; the storage decision lives entirely in infrastructure, which detects `IEventSourcedAggregateRoot`.
- Choosing a base is now a modeling decision made by the domain author, and it does **not** determine or reveal storage.
- Two base classes must be taught and tooled instead of one, and migrating an aggregate between the state-modeled and event-modeled worlds means changing its base class (an intentional, explicit act rather than a silent repository swap).
- Because the package is not yet consumed by any service, this restructure has zero blast radius.

## Alternatives considered

- **Keep the unified base (ADR-0011):** rejected — it forces event-sourcing mechanics onto every aggregate and leaks a storage vocabulary into the domain, defeating selective event sourcing.
- **One base with opt-in `TState`/`Apply` machinery:** less discoverable; the presence or absence of event modeling becomes a runtime convention rather than a clear type choice.
- **Drive the split by storage (an "EF aggregate" vs an "ES aggregate"):** rejected — that is Decision B and must not live in the domain; it would re-introduce the exact leak this ADR removes.
