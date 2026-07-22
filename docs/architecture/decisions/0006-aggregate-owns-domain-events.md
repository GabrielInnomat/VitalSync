# 0006. Aggregate owns its domain events

- **Status:** Accepted
- **Date:** 2026-06-24
- **Amended:** 2026-07-21 (implementation signatures clarified; **later corrected by the 2026-07-22 amendment below** — see [ADR-0012](./0012-optional-event-sourcing-aggregate.md))
- **Amended:** 2026-07-22 (aggregate hierarchy split into two bases; the raise mechanism differs per base — see the note below and [ADR-0012](./0012-optional-event-sourcing-aggregate.md))

## Context

Domain events represent business-relevant occurrences inside an aggregate. The architecture mandates that *adding and removing* domain events is exclusively the responsibility of the aggregate, and that no other layer may tamper with the event stream.

A naive implementation exposes a mutable event collection or a public clear method, which lets any layer add, remove, or clear events at the wrong time — most dangerously, clearing events before they have been dispatched.

## Decision

The aggregate base is the **sole owner** of its domain events:

- Events are stored in a private list.
- Events can only be added via a `protected` raise method, so only the aggregate itself raises events.
- The event collection is exposed only as a read-only view (`IReadOnlyCollection<IDomainEvent> DomainEvents`).
- Clearing is **not** part of the aggregate's public surface (see [ADR-0007](./0007-read-only-vs-managed-domain-events.md)).

> **Implementation note (amendment 2026-07-22, supersedes the 2026-07-21 note):** This ADR originally referred to a single aggregate base `AggregateRoot<TKey>` with a `RaiseDomainEvent(...)` method. The 2026-07-21 amendment updated that to a single unified `AggregateRoot<TKey, TState>` base with `RaiseEvent(...)`. That unified base was subsequently **split into two bases** (see [ADR-0012](./0012-optional-event-sourcing-aggregate.md)):
>
> - **`AggregateRoot<TKey>`** — state-modeled aggregates record events via the protected `AddDomainEvent(IDomainEvent)` method.
> - **`EventSourcedAggregateRoot<TKey, TState>`** — event-modeled aggregates record events via the protected `RaiseEvent(IDomainEvent, IClock)` method, which also applies the event to the state.
>
> The ownership rule described here is unchanged for both bases: events live in a private list, are added only through a `protected` method, and are exposed read-only. Only the type names and the specific raise method differ.

## Consequences

- The "aggregate owns its events" rule is enforced structurally, not by convention.
- Application/handler code cannot tamper with the event stream.
- Reading events is side-effect free, enabling safe inspection (logging, tests, debugging).
- The aggregate cannot clear its own events through the instance surface; clearing is delegated to a privileged contract used by infrastructure.

## Alternatives considered

- **Public mutable list / public `Clear`:** rejected — allows any layer to corrupt the event stream or drop undispatched events.
- **Drain-on-read (`GetDomainEvents()` that clears):** rejected — a query with a hidden side effect; loses events if a later save/dispatch fails, and is surprising to callers.
