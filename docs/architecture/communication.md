# Communication

This document describes how the parts of VitalSync talk to each other. These rules are **mandatory**.

## Rules

1. **Frontend → BFF only.** The Blazor frontend communicates exclusively through the Backend-for-Frontend. It never calls a microservice directly.
2. **BFF → Frontend via REST.** The BFF exposes REST (HTTP/JSON) endpoints consumed by the Blazor client.
3. **BFF → Microservices via code-first gRPC.** Communication between the BFF and the individual microservices uses code-first gRPC (contracts defined in C#, not `.proto` files authored by hand).
4. **Microservice ↔ Microservice is asynchronous only.** There is **no** direct synchronous service-to-service communication. All inter-service communication uses an asynchronous messaging platform.

## Diagram

```text
┌──────────────┐   REST/HTTP    ┌──────────────┐   code-first gRPC    ┌───────────────┐
│   Blazor     │ ─────────────► │     BFF      │ ───────────────────► │ Microservice  │
│  (frontend)  │ ◄───────────── │              │ ◄─────────────────── │               │
└──────────────┘                └──────────────┘                      └──────┬────────┘
                                                                             │
                                                            asynchronous     │  (events / messages)
                                                            messaging only   ▼
                                                                      ┌───────────────┐
                                                                      │ Microservice  │
                                                                      └───────────────┘
```

## Why these boundaries?

- **Single entry point (BFF):** keeps the frontend simple, centralizes cross-cutting concerns (auth, aggregation, shaping), and decouples UI from service topology.
- **Code-first gRPC for BFF↔services:** strongly typed, high-performance contracts authored in C#, kept close to the consuming code.
- **Asynchronous-only between services:** maximizes loose coupling and independent deployability; prevents distributed call chains and temporal coupling.

## Synchronous vs. asynchronous — summary

| Hop | Style | Protocol |
|---|---|---|
| Frontend → BFF | Synchronous (request/response) | REST (HTTP/JSON) |
| BFF → Microservice | Synchronous (request/response) | Code-first gRPC |
| Microservice → Microservice | **Asynchronous** | Messaging (RabbitMQ via MassTransit) |

## Integration events

Cross-service communication is expressed via **integration events** published to the messaging backbone. Integration events are distinct from **domain events** (which are internal to a service's domain model).

See [Domain model](./domain-model.md) for domain events and [Building Blocks](./building-blocks.md) for the outbox/dispatch abstractions.

## Messaging platform

The messaging platform is **RabbitMQ**, accessed through the **MassTransit** abstraction (publish/subscribe, transactional outbox, retries, dead-lettering). The decision and its trade-offs are recorded in [ADR-0004](./decisions/0004-asynchronous-messaging-between-services.md).
