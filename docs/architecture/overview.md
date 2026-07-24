# Architecture Overview

VitalSync is a **cloud-native, distributed application**. This document gives the high-level picture; deeper topics live in their own documents linked below.

## Logical layers

VitalSync is composed of four logical tiers:

1. **Frontend (Blazor)** — user interface only. Holds no business logic and talks exclusively to the BFF.
2. **Backend-for-Frontend (BFF)** — exposes REST to the frontend and orchestrates calls to microservices using code-first gRPC.
3. **Microservices** — one per business area (Nutrition, Fitness, Analytics). Each is independent and owns its data.
4. **Messaging backbone** — the only channel for inter-service communication; strictly asynchronous.

```text
Blazor ──REST──> BFF ──gRPC──> Microservices ──async messaging──> Microservices
```

## Architectural principles

- **Domain-Driven Design (DDD)** in every microservice.
- **CQRS** to separate write and read concerns in every service.
- **Event Sourcing** _where it provides business value_; otherwise traditional persistence with **Entity Framework Core**.
- **Clear separation of business domains** — no shared domain models across contexts.
- **Independent deployability** — services do not share databases and avoid synchronous coupling.

## Non-functional requirements

The architecture must be:

- modular
- extensible
- maintainable
- testable
- loosely coupled
- cloud-native
- reusable in the long term

## The Building Blocks platform

A reusable set of shared Building Blocks underpins the services without coupling them to VitalSync. It covers Domain, Application, Infrastructure, Persistence, and Event Processing. See [Building Blocks](./building-blocks.md).

## Related documents

- [Communication](./communication.md)
- [Building Blocks](./building-blocks.md)
- [Domain model](./domain-model.md)
- [CQRS & Event Sourcing](./cqrs-and-event-sourcing.md)
- [Testing strategy](./testing-strategy.md)
- [Architecture Decision Records](./decisions/README.md)

## Open questions

These are intentionally unresolved and tracked for the "analyze & challenge" phase:

- **Event Sourcing scope**: which Bounded Contexts justify it (see [CQRS & Event Sourcing](./cqrs-and-event-sourcing.md)).
- **Bounded Context boundaries**: the final decomposition is part of the project.

> **Resolved:** The messaging platform is decided — **RabbitMQ** (with MassTransit) — see [ADR-0004](./decisions/0004-asynchronous-messaging-between-services.md).
