# 0007. Read-only vs. managed domain events

- **Status:** Accepted
- **Date:** 2026-06-24
- **Amended:** 2026-08-03 (the managed interface is renamed to `IDomainEventOwner` — see the note below)

## Context

Following [ADR-0006](./0006-aggregate-owns-domain-events.md), the aggregate owns its events. However, **some** component must still be able to clear events after they have been dispatched — and that must happen **only after a successful save**, never earlier. Exposing a public `ClearDomainEvents()` would let any layer clear prematurely and silently drop undispatched events.

We need to separate the *read* capability (available to everyone) from the *clear* capability (available only to infrastructure, at the right time).

## Decision

Split the responsibility across two interfaces and use **explicit interface implementation** for clearing:

- `IHasDomainEvents` — read-only: `IReadOnlyCollection<IDomainEvent> DomainEvents { get; }`.
- `IDomainEventsManager : IHasDomainEvents` — adds `void ClearDomainEvents()`.

Then:

- `IAggregateRoot<TKey>` inherits **only** `IHasDomainEvents`.
- `AggregateRoot<TKey>` additionally implements `IDomainEventsManager`, but **explicitly**, so `ClearDomainEvents()` is not visible on the aggregate's normal surface.

```csharp
void IDomainEventsManager.ClearDomainEvents() => _domainEvents.Clear();
```

To clear, a caller must deliberately obtain the `IDomainEventsManager` view:

```csharp
((IDomainEventsManager)aggregate).ClearDomainEvents();
```

The persistence layer collects events from `IHasDomainEvents`, and — only after `SaveChanges` succeeds — clears them through `IDomainEventsManager`.

> **Naming (amendment 2026-08-03).** `IDomainEventsManager` is renamed to **`IDomainEventOwner`**. Everything above
> holds verbatim with the new name substituted: same split, same explicit implementation, same members, same
> consumers. Only the label changes.
>
> Two reasons. First, "Manager" says nothing — it names no capability, and the interface has exactly one member.
> `Owner` names it: the aggregate *owns* its domain events, which is literally the title of
> [ADR-0006](./0006-aggregate-owns-domain-events.md), so the type now cites the decision it implements. Second, and
> more useful in practice, it makes the Building Blocks domain interfaces legible as a system rather than as sprawl:
>
> | | implemented by | visibility |
> | --- | --- | --- |
> | `IState<TSelf, TKey>` | the state | public, its own axis |
> | `IHasDomainEvents` | the aggregate | public, part of `IAggregateRoot` |
> | `IDomainEventOwner` | the aggregate | **explicit, infrastructure only** |
> | `IStateOwner` | the aggregate | **explicit, infrastructure only** |
>
> The `*Owner` suffix now reads as a pattern — "privileged view, explicitly implemented, infrastructure only" — which
> is what a reader needs to know before touching any of them. A merge of the two `*Owner` interfaces was considered
> and **rejected**: `IDomainEventOwner` applies to every aggregate on both persistence paths, while `IStateOwner` is
> meaningful only for state-stored ones, so `MartenAggregateTracker` would end up declaring a dependency it never
> uses. Four precise contracts beat three imprecise ones; interface count was never the problem.

## Consequences

- Application/handler code holding an aggregate (or `IAggregateRoot<TKey>`) **cannot** call `ClearDomainEvents()`; attempting to do so is a compile error.
- The privileged clear capability is reachable only by code that intentionally casts to the manager interface — by convention, the persistence layer.
- The naming makes the asymmetry explicit: `IHasDomainEvents` (read) vs. `IDomainEventsManager` (lifecycle authority).
- A future, stronger guarantee (e.g. writing events to a transactional outbox in the same `SaveChanges`) can be layered on without changing this contract.

## Alternatives considered

- **Single interface with a public clear:** rejected — defeats the purpose; any layer can clear.
- **`internal` clear + `InternalsVisibleTo`:** viable, but exposes *all* internals to the persistence assembly and ties the domain to a named assembly; less precise than explicit implementation.
- **Marker/token-protected clear:** more ceremony than warranted for the current needs; can be revisited if defense-in-depth is required.