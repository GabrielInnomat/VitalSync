# 0032. Child entities raise domain events through their aggregate root

- **Status:** Accepted
- **Date:** 2026-08-04

## Context

ADR-0031 gave an aggregate a child collection that survives a commit: the children live in the
aggregate **state**, map as owned types with their own strongly typed key, and are reconciled by
key at any depth. It answered the persistence half of the question and deliberately left the
modelling half alone — the sample's `WidgetPart` was a bare record, and every rule about a part
lived on `Widget`.

That is fine for one field and wrong for a real bounded context. A child with its own invariants
("a portion cannot exceed its recipe's yield") pushes those rules up into the root, where they mix
with rules that have nothing to do with them. The DDD answer is an entity inside the aggregate that
owns its own behaviour. The open question was where its **uncommitted domain events** go.

The obvious move — give the child its own event list, or let the state record events during `Apply`
— was measured against three properties this architecture already depends on:

- **Ordering.** ADR-0006 keeps one list on the root, so the append order into Marten and the outbox
  is exactly the order the domain raised them. Several lists have to be merged, and there is no
  field to merge them by: `EventId`/`OccurredAt` are minted at commit, not at raise (ADR-0029).
- **Record equality.** A state is a record. Uncommitted events inside it would take part in
  structural equality and in `with`-copies, so two states with the same data would compare unequal
  because one carries a pending event.
- **Snapshot safety.** `IStateOwner.State` is what gets persisted (ADR-0025). Events in the state
  would be written to the database, and a snapshot would replay them on load.

## Decision

**A child entity has its own state and its own behaviour, but raises through the root.**

- `EntityState<TSelf, TKey>` is the child counterpart of `AggregateState<TSelf, TKey>`: an
  immutable record with `Id` and a pure `Apply`. It has **no `Version`** — the version belongs to
  the aggregate and only to the aggregate (ADR-0030).
- `Entity<TKey, TState>` derives from `EntityBase<TKey>` and is the **only** non-aggregate entity
  base: it assigns `Id` in the constructor with the empty-id guard, and entity equality (ADR-0008)
  stays where it always was. It adds two things: `GetCurrentState()`, which looks
  its state up **through the root** instead of holding a copy, and a `protected RaiseEvent` that
  hands the event to the root.
- The channel is `IDomainEventRaiser`, implemented **explicitly** by `AggregateRoot`, like
  `IDomainEventOwner` and `IStateOwner`. `RaiseEvent` on the root stays the single registration
  point, so ordering, the identity guard, the version bookkeeping and `ClearDomainEvents` are
  untouched and know nothing about children.
- The child's state is part of the root state (ADR-0031). `State.Apply` folds it, usually by
  delegating to the child state's own `Apply`. There is **no** second routing step in `ApplyEvent`,
  no second event list, no `ApplyAndRecord`.
- A root exposes children as thin hulls built on demand (`widget.Part(id)`), and the hull's
  constructor is internal to the aggregate's assembly, so nothing outside can fabricate one.
- **Every entity has a state.** The state-less `Entity<TKey>` is deleted, and `Entity<TKey, TState>`
  takes its place directly under `EntityBase<TKey>`. There is no case left for the state-less base:
  the data of an entity inside an aggregate lives in the aggregate's state graph (ADR-0031), so it
  *is* a state; a value with no identity is a value object; and a child with no behaviour needs no
  hull at all, only its record. The whole abstract entity hierarchy is now two classes —
  `EntityBase<TKey>` with exactly two children, `Entity<TKey, TState>` and
  `AggregateRoot<TKey, TState>`. The root cannot join them: its `Id` is derived (`=> State.Id`) and
  its state is an `AggregateState`, which carries the version.

`GetCurrentState()` is a method, not a property, because it can throw: a hull whose child was removed
in the same command has nothing left to read, and a property that throws is exactly what CA1065
forbids. The throw is the point — the alternative is a hull silently reading stale data.

## Consequences

- Uncommitted events stay on the root, so ordering, `ClearDomainEvents`, the Marten append and the
  outbox behave exactly as before. `EfCoreAggregateTracker`, both units of work,
  `AggregateStateGraph` and `IRepository` (ADR-0026) needed **no change at all**.
- A child-only change advances the aggregate version, which is what makes it visible to the EF Core
  concurrency token and to the projection watermark (ADR-0030/0031). This was already true for a
  root-raised child event and stays true when the child raises it.
- Rehydration is unchanged in both directions: `LoadFromHistory` folds child events like any other,
  and `IStateOwner.Restore` brings the children back because they are in the state document. Hulls
  built after a restore read live data.
- A hull is a view, not a cached copy: two hulls for the same child are equal, never the same
  instance, and both see the latest state. Reading one for a removed child throws
  `DomainValidationException` instead of returning stale values.
- `IDomainEventRaiser` is public, so `((IDomainEventRaiser)aggregate).Raise(e)` is technically
  reachable from application code. That is the same exposure `IDomainEventOwner.ClearDomainEvents`
  already has and the same trade-off: an internal interface cannot appear in the `protected`
  constructor of a public base class.
- Both samples show the pattern end to end. `WidgetPart` became a hull over `WidgetPartState` with
  **no schema change and no migration** — the owned mapping already pointed at the record that is
  now the child state. `Gadget` gained `GadgetComponent`, which demonstrates the command path and
  the replay path in an event-sourced aggregate.
- Removing `Entity<TKey>` costs the equality test doubles a state each — they now exercise the type
  the production code actually derives from. It also amends ADR-0008 and ADR-0025, which named the
  state-less base by hand; both rules (constructor-assigned identity with an `IsEmpty` guard, one
  equality implementation on `EntityBase<TKey>`) survive the move unchanged.

## Alternatives considered

- **Uncommitted events in the state object.** Rejected for the three reasons above (ordering,
  record equality, snapshot safety). It would also require a second `Apply` mode
  (`ApplyAndRecord`), which splits the one place where the fold happens.
- **A private event list per child, merged at commit.** Same ordering problem, plus a merge key
  that does not exist before commit, plus `ClearDomainEvents` becoming a walk over the child graph
  instead of `_domainEvents.Clear()`.
- **A child entity mapped as an independent entity type with its own repository access.** Rejected
  by ADR-0031 already — it dissolves the aggregate boundary and is refused at host startup.
- **Leaving all behaviour on the root.** This is the status quo ADR-0031 left behind. It works for
  a flat child record and stops working as soon as the child has invariants of its own; the root
  then accumulates rules that are not about the root.
- **Keeping `Entity<TKey>` next to `Entity<TKey, TState>`.** Rejected: three abstract entity bases
  for two roles. The state-less one had no production deriver — only two equality test doubles —
  and its guard was already duplicated in `AggregateRoot.ApplyEvent` and `IStateOwner.Restore`, so
  it centralised nothing.
- **Letting `AggregateState<TSelf, TKey>` derive from `EntityState<TSelf, TKey>`** to drop the
  duplicated `Id`/`Apply` declarations. It compiles, but then an aggregate state satisfies the child
  constraint `where TState : EntityState<TState, TKey>`, so a hull could be declared over the state
  of another aggregate. Three saved lines are not worth losing that compile-time separation.
