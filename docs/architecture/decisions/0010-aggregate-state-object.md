# 0010. Aggregate state object

- **Status:** Accepted
- **Date:** 2026-06-25

## Context

When an aggregate participates in event sourcing, two methods tend to appear per domain event: one on the public command API (the behavior a command invokes) and one that applies the event to mutate state (used both for new events and for replay during rehydration). For a large aggregate with many event types, this "two methods per event" growth makes the aggregate class large and interleaves two concerns — *deciding what should happen* and *how state reflects what happened* — in a single file.

Additionally, the platform requires a single aggregate model that works for both event-sourced and state-stored (EF Core) persistence (see [ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)). A common representation of "current state" is needed that both worlds can use.

## Decision

Introduce a dedicated **state object** per aggregate, described by `IState<TKey>`:

```csharp
public interface IState<out TKey> where TKey : struct, IEntityKey
{
    TKey Id { get; }
    IState<TKey> Apply(IDomainEvent domainEvent);
}
```

- **All apply/evolution logic lives on the state.** The aggregate (`AggregateRoot<TKey, TState>`) exposes only the public command API and delegates state changes to the state's `Apply`.
- **The state owns the aggregate's identity** (`Id`). The aggregate derives its `Id` from the state.
- **State objects are immutable**: `Apply` returns the next state (`this with { … }`).
- The single base `AggregateRoot<TKey, TState>` calls `Apply` in `RaiseEvent` (new events) and `LoadFromHistory` (replay), advances `Version`, validates identity at each transition, and records uncommitted events.
- Apply logic is **on the state itself**, not in a separately injected applier.

## Consequences

- Large aggregates stay readable: the aggregate file contains business behavior; the state file contains evolution logic.
- The state object is a small, individually testable unit (state + event → next state).
- The same state type is the natural representation persisted by EF Core and rebuilt by replay in event sourcing, enabling a single aggregate model (ADR-0011).
- Aggregate identity cannot be set at construction; it is established by the first applied event and validated per transition (see [ADR-0008](./0008-entity-identity-and-equality.md)).
- Reading aggregate state at command-time goes through `State.X` (e.g. invariant checks read from `State`), a minor ergonomic shift.
- Mapping an immutable state record (including collections) in EF Core requires owned/complex-type configuration; this cost lives in the Persistence building block, not the domain.

## Alternatives considered

- **Apply-on-the-aggregate** (mutating methods directly on the aggregate): simplest, but produces the "two methods per event" growth and interleaves concerns in large aggregates.
- **Injected `IEventApplier<TState>`** (apply logic in a separate injected service): adds constructor ceremony to every aggregate and splits a state and its evolution across two types; rejected in favor of apply-on-the-state.
- **No shared state type** (each world models state differently): defeats the goal of a single aggregate model for ES and EF Core.