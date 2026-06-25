# 0006. Aggregate owns its domain events

- **Status:** Accepted
- **Date:** 2026-06-24

## Context

Domain events represent business-relevant occurrences inside an aggregate. The architecture mandates that *adding and removing* domain events is exclusively the responsibility of the aggregate, and that other layers may access these events only in a read-only manner.

A naive implementation exposes a mutable event collection or a public clear method, which lets any layer add, remove, or clear events at the wrong time — most dangerously, clearing events before they have been dispatched after a successful save.

## Decision

`AggregateRoot<TKey>` is the **sole owner** of its domain events:

- Events are stored in a private list.
- Events can only be added via a `protected RaiseDomainEvent(...)`, so only the aggregate itself raises events.
- The event collection is exposed only as a read-only view (`IReadOnlyCollection<IDomainEvent> DomainEvents`).
- Clearing is **not** part of the aggregate's public surface (see [ADR-0007](./0007-read-only-vs-managed-domain-events.md)).

## Consequences

- The "aggregate owns its events" rule is enforced structurally, not by convention.
- Application/handler code cannot tamper with the event stream.
- Reading events is side-effect free, enabling safe inspection (logging, tests, debugging).
- The aggregate cannot clear its own events through the instance surface; clearing is delegated to a privileged contract used by infrastructure.

## Alternatives considered

- **Public mutable list / public `Clear`:** rejected — allows any layer to corrupt the event stream or drop undispatched events.
- **Drain-on-read (`GetDomainEvents()` that clears):** rejected — a query with a hidden side effect; loses events if a later save/dispatch fails, and is surprising to callers.