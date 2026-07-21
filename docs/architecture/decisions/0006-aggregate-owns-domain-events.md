# 0006. Aggregate owns its domain events

- **Status:** Accepted
- **Date:** 2026-06-24
- **Amended:** 2026-07-21 (implementation signatures clarified — the aggregate base is `AggregateRoot<TKey, TState>` and the raise method is `RaiseEvent(...)`; see the note below and [ADR-0010](./0010-aggregate-state-object.md))

## Context

Domain events represent business-relevant occurrences inside an aggregate. The architecture mandates that *adding and removing* domain events is exclusively the responsibility of the aggregate, and no outside layer may tamper with the event stream.

A naive implementation exposes a mutable event collection or a public clear method, which lets any layer add, remove, or clear events at the wrong time — most dangerously, clearing events before they have been dispatched.

## Decision

The aggregate base is the **sole owner** of its domain events:

- Events are stored in a private list.
- Events can only be added via a `protected` raise method, so only the aggregate itself raises events.
- The event collection is exposed only as a read-only view (`IReadOnlyCollection<IDomainEvent> DomainEvents`).
- Clearing is **not** part of the aggregate's public surface (see [ADR-0007](./0007-read-only-vs-managed-domain-events.md)).

> **Implementation note (amendment 2026-07-21):** This ADR originally referred to the aggregate base as `AggregateRoot<TKey>` with a `RaiseDomainEvent(...)` method. The design later evolved (see [ADR-0010](./0010-aggregate-state-object.md) and [ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)) so that the state object owns identity and apply logic. The **as-implemented** signatures are:
>
> - the base class is `AggregateRoot<TKey, TState>` (not `AggregateRoot<TKey>`), and
> - the protected raise method is `RaiseEvent(IDomainEvent)` (not `RaiseDomainEvent(...)`).
>
> The ownership rule described here is unchanged; only the type/method names differ from the original wording.

## Consequences

- The "aggregate owns its events" rule is enforced structurally, not by convention.
- Application/handler code cannot tamper with the event stream.
- Reading events is side-effect free, enabling safe inspection (logging, tests, debugging).
- The aggregate cannot clear its own events through the instance surface; clearing is delegated to a privileged contract used by infrastructure.

## Alternatives considered

- **Public mutable list / public `Clear`:** rejected — allows any layer to corrupt the event stream or drop undispatched events.
- **Drain-on-read (`GetDomainEvents()` that clears):** rejected — a query with a hidden side effect; loses events if a later save/dispatch fails, and is surprising to callers.
