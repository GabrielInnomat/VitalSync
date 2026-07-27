# 0023. Wolverine as the messaging transport (replaces MassTransit)

- **Status:** Accepted
- **Date:** 2026-07-27
- **Supersedes:** [ADR-0004](./0004-asynchronous-messaging-between-services.md)

## Context

[ADR-0004](./0004-asynchronous-messaging-between-services.md) adopted
**asynchronous messaging** as the **only** mechanism for inter-service
communication, using **RabbitMQ** as the broker and **MassTransit** as the
abstraction on top of it (publish/subscribe, transactional outbox, retries,
dead-lettering).

Two things changed since then:

- **MassTransit is moving to a commercial license** for its future versions.
  VitalSync has an established principle of avoiding paid or restrictively
  licensed dependencies for fundamental, cross-cutting concerns — the same
  driver behind [ADR-0014](./0014-replace-fluentassertions-with-xunit-asserts.md)
  (removed FluentAssertions) and [ADR-0015](./0015-hand-rolled-cqrs-mediator.md)
  (avoided MediatR). Messaging is exactly such a cross-cutting concern.
- The event store is now **Marten on PostgreSQL**
  ([ADR-0019](./0019-event-store-technology-marten.md)). **Wolverine** is from
  the same ecosystem as Marten and is designed to run **side-by-side** with it,
  sharing a transaction and a **native, PostgreSQL-backed transactional
  outbox**. This lets the outbox write and the domain write commit in the
  **same unit-of-work transaction**, with delivery happening reliably after
  commit.

This decision affects the architecture's transport backbone and therefore
warrants a superseding ADR rather than an edit to ADR-0004.

## Decision

Replace **MassTransit with Wolverine** as the messaging library, while keeping
everything else from ADR-0004 intact.

- **Unchanged from ADR-0004:** asynchronous messaging remains the **only**
  mechanism for inter-service communication; **RabbitMQ** remains the broker,
  provisioned as a first-class **.NET Aspire** resource; only **integration
  events** cross the broker; domain-event replay stays an internal, per-service
  event-store concern.
- **Changed:** the messaging abstraction on top of RabbitMQ is **Wolverine**
  (publish/subscribe, transactional outbox, retries, dead-lettering) instead of
  MassTransit. Wolverine is **MIT-licensed** and runs **side-by-side with
  Marten** (ADR-0019).
- **Outbox in the unit of work.** Integration-event signals are enqueued to
  Wolverine's outbox **inside the write transaction** (the same unit of work
  that persists the aggregate change), so they are captured **atomically** with
  the state change. After commit, Wolverine reliably delivers them to RabbitMQ.
  This is the mechanism ADR-0022 already relies on; only the concrete library
  behind the outbox changes.
- **Placement is unchanged** ([ADR-0018](./0018-three-building-block-packages.md)):
  Wolverine lives **only** in `BuildingBlocks.Infrastructure` (and the service
  hosts). `BuildingBlocks.Domain` and `BuildingBlocks.Application` stay
  **framework-agnostic** and take **no** dependency on Wolverine.

> **Scope note — Wolverine is the transport, not the mediator.** Wolverine is
> adopted **only** as the inter-service messaging transport. It is **not** used
> as the in-process CQRS mediator: the hand-rolled `ISender` dispatcher of
> [ADR-0015](./0015-hand-rolled-cqrs-mediator.md) stays in place. Where an
> incoming Wolverine message must trigger domain work, its handler is a **thin
> adapter** that calls `ISender`, so the `Result` model, exception-to-Result
> translation (ADR-0017), and pipeline-behavior ordering remain authoritative
> and the framework-agnostic core stays decoupled from the transport.

## Consequences

- **Easier / better:** no paid or restrictively licensed messaging dependency;
  a single ecosystem (Wolverine + Marten) with **native, same-transaction
  outbox** support, removing the cross-store friction of gluing a separate
  outbox to Marten; MIT licensing keeps the cross-cutting transport concern free
  of license risk.
- **Preserved:** the communication topology, the RabbitMQ broker, the
  integration-event-only boundary, the Aspire wiring, and the CQRS/mediator
  architecture (ADR-0015) are all unchanged.
- **Harder / migration cost:** existing MassTransit wiring, message
  registration, and any consumer configuration must be ported to Wolverine's
  APIs; contributors must not reintroduce MassTransit.
- **Exit strategy retained (as in ADR-0004):** should a concrete, high-volume
  **stream-processing** requirement emerge (e.g. in Analytics), the transport
  can be re-evaluated behind a new superseding ADR.

## Alternatives considered

- **Keep MassTransit** — rejected: moving to a commercial license, the exact
  situation ADR-0014/0015 established we avoid for cross-cutting concerns.
- **Wolverine (chosen)** — MIT-licensed, same ecosystem as Marten (ADR-0019),
  runs side-by-side with it and shares a **native transactional outbox** in the
  same PostgreSQL transaction as the write, which is precisely the
  outbox-in-unit-of-work behavior VitalSync wants.
- **Also use Wolverine as the CQRS mediator** — rejected: it would couple the
  framework-agnostic `BuildingBlocks.Application` layer to Wolverine and force
  VitalSync's `Result`/exception/pipeline conventions to fit Wolverine's
  opinions, re-introducing exactly the coupling ADR-0015 avoids. The hand-rolled
  dispatcher is kept; Wolverine is transport-only.
- **Other brokers / abstractions (Kafka, raw client, NServiceBus, etc.)** —
  out of scope here: ADR-0004's broker analysis (RabbitMQ for human-paced,
  routing-centric load) is unchanged; this ADR only swaps the **abstraction
  library**, not the broker.

## Revisit criteria

The revisit criteria from [ADR-0004](./0004-asynchronous-messaging-between-services.md)
carry over unchanged (stream processing, durable long-term replay of
integration events, or throughput beyond a single RabbitMQ broker).
