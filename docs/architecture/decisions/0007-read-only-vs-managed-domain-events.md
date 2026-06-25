# 0007. Read-only vs. managed domain events

- **Status:** Accepted
- **Date:** 2026-06-24

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

## Consequences

- Application/handler code holding an aggregate (or `IAggregateRoot<TKey>`) **cannot** call `ClearDomainEvents()`; attempting to do so is a compile error.
- The privileged clear capability is reachable only by code that intentionally casts to the manager interface — by convention, the persistence layer.
- The naming makes the asymmetry explicit: `IHasDomainEvents` (read) vs. `IDomainEventsManager` (lifecycle authority).
- A future, stronger guarantee (e.g. writing events to a transactional outbox in the same `SaveChanges`) can be layered on without changing this contract.

## Alternatives considered

- **Single interface with a public clear:** rejected — defeats the purpose; any layer can clear.
- **`internal` clear + `InternalsVisibleTo`:** viable, but exposes *all* internals to the persistence assembly and ties the domain to a named assembly; less precise than explicit implementation.
- **Marker/token-protected clear:** more ceremony than warranted for the current needs; can be revisited if defense-in-depth is required.