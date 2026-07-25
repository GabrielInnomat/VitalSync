# 0019. Marten on PostgreSQL as the event store

- **Status:** Accepted
- **Date:** 2026-07-25

## Context

VitalSync applies **selective Event Sourcing** — EF Core is the default, and an
aggregate is event-sourced only where the event history carries business value
(see [ADR-0012](./0012-optional-event-sourcing-aggregate.md)). Two questions were
left explicitly open in
[CQRS & Event Sourcing](../cqrs-and-event-sourcing.md):

1. Which bounded contexts justify Event Sourcing? *(still open — decided per context)*
2. **Which event store technology backs the event-sourced contexts?** *(this ADR)*

The domain already fixes the shape of an event-sourced aggregate and must not be
bent to fit a tool:

- `EventSourcedAggregateRoot<TKey, TState>` keeps all evolution logic on an
  immutable **state object** (`IState<TSelf, TKey>.Apply`, see
  [ADR-0010](./0010-aggregate-state-object.md)), **not** as `Apply` methods on
  the aggregate.
- Rehydration goes through `LoadFromHistory(IEnumerable<IDomainEvent>)`, and the
  event-sourcing members (`Version`, `LoadFromHistory`) are **explicitly
  implemented** behind an `IEventSourcedAggregateRoot<TKey>` cast so they never
  appear on an aggregate's public surface (see
  [ADR-0007](./0007-read-only-vs-managed-domain-events.md) and ADR-0012).

Any event store therefore has to be usable as a **raw stream store** that we fold
ourselves via `LoadFromHistory`. It must additionally be **free to use**, ship a
**first-party .NET Aspire integration**, and allow **snapshotting and
historisation** to be added **without changing the event schema or migrating
existing streams**.

### Options evaluated

The choice narrowed to **EventStoreDB (Kurrent)** vs **Marten (on PostgreSQL)**.
A hand-written store was rejected up front as too much ongoing work.

| Concern | EventStoreDB / Kurrent | Marten (on PostgreSQL) |
| ------- | ---------------------- | ---------------------- |
| `LoadFromHistory` fit | Excellent — no aggregation opinion; read stream, fold ourselves | Good, but its `Apply`-scanning convention must be **bypassed** via `FetchStream` |
| Versioning / optimistic concurrency | Native (`expectedRevision` on append) | Native (expected version on append; `ConcurrencyException`) |
| Snapshotting | **Not built in** — must hand-roll a snapshot stream | **Built-in** aggregate snapshots; also easy to hand-roll as a separate document |
| Historisation / temporal | Native append-only streams, point-in-time reads | Native (`AggregateStreamToVersion`, archiving, time-travel) |
| License (free) | Licensing shifted; free tier terms need verifying | **MIT — unambiguously free** |
| Aspire integration | Community / third-party only | **First-party** via `Aspire.Hosting.PostgreSQL` (Marten is a NuGet lib on top) |

## Decision

Adopt **Marten on PostgreSQL** as the event store for event-sourced bounded
contexts, used as a **raw event store** rather than through its convention-based
aggregation:

- **Writes** append the aggregate's uncommitted domain events to its stream with
  optimistic concurrency asserted against the aggregate's `Version`.
- **Reads** fetch the raw event stream (`FetchStream`) and fold it through our own
  `((IEventSourcedAggregateRoot<TKey>)aggregate).LoadFromHistory(history)`. We do
  **not** use Marten's `Apply`-on-aggregate convention, so ADR-0010 and ADR-0012
  are preserved unchanged.
- The Marten dependency lives **only** in `BuildingBlocks.Infrastructure` (its
  intended home for third-party, framework-bound implementations, see
  [ADR-0018](./0018-three-building-block-packages.md)); `Domain` and `Application`
  stay dependency-free.
- **Snapshotting is deferred, not designed out.** We ship without snapshots. If a
  context later needs them, they are added as a **separate snapshot document** —
  read the latest snapshot, then `FetchStream` the tail from the snapshot version
  and `LoadFromHistory` the remainder. Because a Marten snapshot is a distinct
  document table and the event schema (`mt_events` / `mt_streams`) is identical
  with or without snapshots, **adding snapshotting later is a purely additive
  change with no event migration.**
- To support snapshot rehydration when needed, the event-sourced base gains a
  small, additive **"seed from `State`"** extension point (e.g. a `FromState`
  factory) so an aggregate can start replay from a snapshot state instead of from
  zero. This does not break the existing replay-from-zero path.

## Consequences

- One datastore technology (PostgreSQL) backs both EF Core (default) and
  event-sourced contexts, with a **first-party Aspire hosting integration** and a
  clear **MIT** license.
- The event-sourced repository in `BuildingBlocks.Infrastructure` is small
  (roughly one class): read stream → `LoadFromHistory`, append uncommitted events
  with expected-version concurrency mapped onto our `Version`.
- The domain is untouched: no `Apply`-on-aggregate convention leaks in, and the
  explicit-interface hiding of `Version`/`LoadFromHistory` is preserved.
- Snapshotting can be turned on later per context **without a schema change or
  event migration** — satisfying the "add snapshotting if needed without breaking
  or changing the DB" requirement.
- We take on a bounded, one-time adapter that deliberately sidesteps some of
  Marten's convenience API; we accept Marten as a third-party dependency in
  Infrastructure, which is exactly what that package exists for.

## Alternatives considered

- **EventStoreDB / Kurrent:** the most natural `LoadFromHistory` fit (no
  aggregation opinion to bypass) and a purpose-built event store, but it has **no
  built-in snapshotting** (forcing hand-written snapshot streams), only a
  **third-party Aspire** integration, and **uncertain licensing** for the free
  tier. Rejected because it is weaker on both hard requirements (free + first-party
  Aspire) and forces hand-written snapshotting.
- **Marten via its native `Apply`-convention / snapshot projections:** least
  hand-written code, but requires bridging Marten's aggregation convention to our
  `State.Apply`, effectively re-introducing `Apply`-on-aggregate mechanics that
  ADR-0010 and ADR-0012 removed. Rejected in favour of using Marten as a raw store.
- **Hand-written event store on EF Core:** full control and zero new third-party
  dependency, but requires building append/concurrency/snapshot/history machinery
  ourselves. Rejected as too much ongoing work.
