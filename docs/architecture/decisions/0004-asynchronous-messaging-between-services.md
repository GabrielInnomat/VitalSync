# 0004. Asynchronous messaging between services

- **Status:** Proposed
- **Date:** 2026-06-23

## Context

To maximize loose coupling and independent deployability, microservices must not call each other synchronously. Inter-service communication is therefore **asynchronous** via a messaging platform. The specification names **Kafka or RabbitMQ** as candidates; the concrete platform is not yet fixed.

## Decision

Adopt **asynchronous messaging** as the **only** mechanism for inter-service communication. The concrete platform (**Kafka** vs. **RabbitMQ**) is **still to be decided**; this ADR will be updated to **Accepted** once that choice is made (or split into a follow-up ADR documenting the selected platform).

## Consequences

- Services are loosely coupled and independently deployable.
- No distributed synchronous call chains or temporal coupling between services.
- Eventual consistency must be designed for explicitly (e.g., via integration events and an outbox).
- Additional operational components (the broker, and possibly schema/registry tooling) must be run and monitored.

## Alternatives considered

- **Synchronous service-to-service calls (gRPC/HTTP):** explicitly rejected by the architecture — introduces tight coupling and cascading failures.
- **Kafka:** strong for high-throughput, log-based streaming and replay; heavier to operate.
- **RabbitMQ:** strong for flexible routing and simpler operational footprint; less suited to large-scale event replay.

> The trade-offs above will be revisited during the "analyze & challenge" phase before this ADR is accepted.