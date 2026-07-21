# 0004. Asynchronous messaging between services

- **Status:** Accepted
- **Date:** 2026-06-25

## Context

To maximize loose coupling and independent deployability, microservices must not call each other synchronously. Inter-service communication is therefore **asynchronous** via a messaging platform. Two candidate platforms were considered: **Apache Kafka** and **RabbitMQ**.

The relevant forces for VitalSync:

- **Load profile is human-paced.** Messages represent real-world user actions ("meal logged", "workout completed", "recipe created") — on the order of hundreds to low-thousands of messages per day, not a high-throughput stream.
- **Routing-oriented, not stream-oriented.** Cross-service communication is expressed as **integration events** with a mix of routing needs (see [communication.md](../communication.md)), which maps naturally onto an exchange/binding model.
- **Small operating team.** Operational simplicity has high value; there is no dedicated platform team to run a partitioned, coordinated log cluster.
- **Aspire-first.** ADR-0002 commits the project to **.NET Aspire 13**, which provides a first-class hosting + client integration for RabbitMQ (resource wiring, health checks, OpenTelemetry).
- **Event Sourcing is an internal store concern, not a transport concern.** Per [cqrs-and-event-sourcing.md](../cqrs-and-event-sourcing.md), Event Sourcing is _selective_ and _internal to a service's write side_; the **event store technology is a separate, still-open decision**. Only **integration events** cross the broker, so a replayable transport log is not required for inter-service messaging.

## Decision

Adopt **asynchronous messaging** as the **only** mechanism for inter-service communication, using **RabbitMQ** as the messaging platform.

- Integration events are published to and consumed from RabbitMQ.
- A messaging abstraction (**MassTransit**) is used on top of RabbitMQ to provide publish/subscribe, the **transactional outbox**, retries, and dead-lettering, and to keep services decoupled from the concrete broker.
- The broker is provisioned as a first-class **.NET Aspire** resource and referenced by each service.

> **Scope note:** This decision governs the **inter-service transport** only. It does **not** decide the **event store** for any Bounded Context that adopts Event Sourcing — that remains an open question in [cqrs-and-event-sourcing.md](../cqrs-and-event-sourcing.md). Conflating "we do Event Sourcing" with "we need a log-based broker" is explicitly rejected: domain-event replay belongs to a per-service event store, not to the integration-event backbone.

## Consequences

- Services are loosely coupled and independently deployable.
- No distributed synchronous call chains or temporal coupling between services.
- Eventual consistency must be designed for explicitly via **integration events** and a **transactional outbox** (see [building-blocks.md](../building-blocks.md)).
- RabbitMQ (plus its management UI) must be run and monitored; in Aspire this is a single resource locally and a standard broker in production.
- The MassTransit abstraction preserves an **exit strategy**: should a concrete, high-volume **stream-processing** requirement emerge (e.g., in Analytics), the transport can be re-evaluated (and potentially swapped) with limited blast radius. Such a change would require a new ADR superseding this one.
- gRPC and broker telemetry are unified through OpenTelemetry in `ServiceDefaults`.

## Alternatives considered

- **Synchronous service-to-service calls (gRPC/HTTP):** explicitly rejected by the architecture — introduces tight coupling and cascading failures.
- **Apache Kafka:** strong for high-throughput, log-based streaming and arbitrary replay; would be the right choice if integration events themselves needed long-term retention/replay, or if Analytics required on-the-wire stream processing (windowing/aggregations). For VitalSync's human-paced load and routing-centric event model, Kafka's strengths are unused while its operational overhead (partitions, coordination, consumer-group management) is a net cost. It also has a more peripheral self-hosted story in .NET Aspire compared to RabbitMQ.
- **RabbitMQ (chosen):** flexible routing via exchanges/bindings, simpler operational footprint, first-class Aspire integration, and an idiomatic .NET + MassTransit experience. Less suited to large-scale event replay — which is not required for the inter-service transport.

## Revisit criteria

Re-open this decision (via a superseding ADR) if **any** of the following becomes true:

- The Analytics context requires real **stream processing** over high-volume event streams.
- Integration events must be **durably retained and replayable** for long periods (e.g., to rebuild a brand-new service purely from history).
- Sustained message throughput grows beyond what a single RabbitMQ broker comfortably handles.
