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
| Microservice → Microservice | **Asynchronous** | Messaging (RabbitMQ via Wolverine) |

## Integration events

Cross-service communication is expressed via **integration events** published to the messaging backbone. Integration events are distinct from **domain events** (which are internal to a service's domain model).

See [Domain model](./domain-model.md) for domain events and [Building Blocks](./building-blocks.md) for the outbox/dispatch abstractions.

### Broker topology

All integration events are published to the single topic exchange **`vitalsync.integration-events`**, which Wolverine provisions automatically. The routing key of an event is its **`[Topic("<context>.<event>")]`** attribute in kebab-case:

```csharp
[Topic("nutrition.recipe-created")]
public sealed record RecipeCreated(Guid RecipeId) : IIntegrationEvent;
```

The attribute is **mandatory** on every integration event. It makes the routing key part of the published contract instead of deriving it from the CLR namespace, where a rename would silently break consumer bindings.

Consumers own their side of the topology: a subscribing service declares its own queue and binds it to the exchange with a topic pattern (`nutrition.*`), so adding a subscriber never changes the publishing service. Building Blocks wires only the publishing half.

Domain events never reach the broker. The publishing rule matches the `IIntegrationEvent` marker, and the `DomainEventEnvelope` that carries domain events through a service's local outbox queue does not implement it — integration events remain the only cross-context signal (ADR-0022).

## Messaging platform

The messaging platform is **RabbitMQ**, accessed through the **Wolverine** abstraction (publish/subscribe, transactional outbox, retries, dead-lettering). Wolverine is MIT-licensed and runs side-by-side with Marten (ADR-0019), enqueuing integration events to its outbox inside the write transaction and delivering them after commit. The decision and its trade-offs are recorded in [ADR-0023](./decisions/0023-wolverine-messaging-transport.md) (which supersedes [ADR-0004](./decisions/0004-asynchronous-messaging-between-services.md)).
