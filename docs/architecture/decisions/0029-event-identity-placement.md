# 0029. Event identity lives in the envelope for domain events and on the event for integration events

- **Status:** Accepted
- **Date:** 2026-08-03

## Context

Until now the two event families placed their identity inconsistently, and in both cases
wrongly:

- `DomainEvent` carried `EventId` (minted via `Guid.NewGuid()` in the constructor) and an
  `OccurredAt` that started as a sentinel (`Ticks == 0`) and was overwritten at commit by an
  infrastructure stamper. The fresh Guid per instance meant two semantically identical domain
  events were never value-equal — defeating the purpose of a record — and the sentinel-based
  stamping was fragile and leaked an infrastructure concern into the domain.
- `IIntegrationEvent` was an empty marker. A consuming context that derives its own aggregates
  from a foreign event had no identity to deduplicate on, even though delivery is
  at-least-once.

Meanwhile `DomainEventEnvelope` — the wrapper every domain event travels in through the
outbox — carried nothing but a type name and a payload, although it is the natural place for
transport metadata.

## Decision

Identity follows the boundary the event crosses:

- **Domain events carry no identity.** `IDomainEvent` and `DomainEvent` are pure, empty
  contracts; concrete events are plain value records with working value equality. `EventId`
  and `OccurredAt` are minted by the unit of work at commit time and travel on the
  `DomainEventEnvelope`, which every domain event passes through anyway. Consumers on the
  publish path receive them as an explicit `DomainEventMetadata` parameter
  (`IDomainEventPublisher.PublishAsync`, `IIntegrationEventMapper.Map`).
- **Integration events carry their identity.** `IIntegrationEvent` requires `Guid EventId`
  and `DateTimeOffset OccurredAt`. Integration events are contracts on the wire; there is no
  envelope the foreign consumer knows about, so the identity must be part of the published
  contract itself. Mappers populate both from the `DomainEventMetadata` they receive, so the
  identity is stable across outbox redeliveries — a mapper must never mint a fresh Guid per
  invocation.

The asymmetry is deliberate and principled, not an inconsistency to be cleaned up later:
domain events always travel inside infrastructure the context owns (an envelope is always
available), integration events are a published contract without a shared envelope.

## Consequences

- Domain events regain value equality; test assertions can compare whole records.
- The sentinel-based `DomainEventStamper` is deleted; there is no post-hoc mutation of events.
- The Marten event stream stores pure business payloads; Marten's own event metadata provides
  store-level identity, and the envelope provides transport identity.
- Consumers of integration events have a stable `EventId` to keep idempotency bookkeeping on,
  which unblocks cross-context deduplication.
- One domain event mapped to several integration events shares one `EventId` across them; if a
  consumer needs to distinguish those, the mapper must derive distinct deterministic ids.
- `IIntegrationEventMapper.Map` and `IDomainEventPublisher.PublishAsync` gained a
  `DomainEventMetadata` parameter — a small, explicit widening of two Application contracts.

## Alternatives considered

- **Identity on both event families.** Cheapest change, but keeps the broken value equality of
  domain events and duplicates metadata into every stored payload.
- **Envelope everywhere, identity nowhere.** Relies on Wolverine's `Envelope.Id` for consumer
  deduplication. Cleanest records, but couples idempotency to the transport: replays and
  republishes mint fresh ids, and non-Wolverine consumers see no identity at all.
