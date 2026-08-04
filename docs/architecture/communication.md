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

### Event identity

Identity placement is deliberately **asymmetric** (ADR-0029): **domain events carry no identity** — they are pure value records, and `EventId`/`OccurredAt` are minted at commit and travel on the `DomainEventEnvelope`, which is always present inside the owning service. **Integration events carry their identity on the event** — `IIntegrationEvent` requires `EventId` and `OccurredAt`, because there is no envelope a foreign consumer knows about and duplicate detection under at-least-once delivery needs a stable id on the contract itself. Mappers populate both from the `DomainEventMetadata` they receive; they must never mint a fresh Guid per invocation, or redeliveries produce new identities and deduplication breaks. Do not "clean up" this asymmetry — it is the decided rule, not an inconsistency.

See [Domain model](./domain-model.md) for domain events and [Building Blocks](./building-blocks.md) for the outbox/dispatch abstractions.

### Broker topology

All integration events are published to the single topic exchange **`vitalsync.integration-events`**, which Wolverine provisions automatically. The routing key of an event is its **`[IntegrationEventTopic("<context>.<event>")]`** attribute in kebab-case — a Building Blocks attribute living in `BuildingBlocks.Application` next to `IIntegrationEvent`, so contract assemblies stay free of any transport dependency (ADR-0023 amendment 2026-08-03):

```csharp
[IntegrationEventTopic("nutrition.recipe-created")]
public sealed record RecipeCreated(Guid RecipeId, Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
```

The attribute is **mandatory** on every integration event. It makes the routing key part of the published contract instead of deriving it from the CLR namespace, where a rename would silently break consumer bindings. Both halves fail fast: the attribute rejects anything but two kebab-case segments at construction, and publishing an event without the attribute throws instead of silently publishing under a key no consumer has bound.

Consumers own their side of the topology in the sense that they choose their queue name and topic patterns; Building Blocks wires **both** halves, so a subscribing host adds nothing of its own: `SubscribeToIntegrationEvents(queueName, consumerAssembly, patterns)` declares the queue, binds it to the exchange, enables the durable inbox and registers the consumer assembly (ADR-0023 amendment 2026-08-01). Adding a subscriber never changes the publishing service.

**Delivery is durable end to end** (ADR-0023 amendment 2026-08-04). The exchange and every queue are declared **durable**, queues are **quorum** queues transport-wide — including Wolverine's `wolverine-dead-letter-queue` — and the sending endpoint to the exchange is **durable**, so an outgoing event is written to `wolverine_outgoing_envelopes` before it is sent and reaches the broker with `delivery_mode: 2`. Neither a broker restart nor a process crash between commit and acknowledgement loses an integration event. Because the durable sending endpoint needs a message store, `UseWolverineMessaging` without `UseEfCorePersistence` or `UseMartenEventSourcing` fails at composition time rather than degrading to a host that only looks durable.

Domain events never reach the broker. The publishing rule matches the `IIntegrationEvent` contract, and the `DomainEventEnvelope` that carries domain events through a service's local outbox queue does not implement it — integration events remain the only cross-context signal (ADR-0022).

## Messaging platform

The messaging platform is **RabbitMQ**, accessed through the **Wolverine** abstraction (publish/subscribe, transactional outbox, retries, dead-lettering). Wolverine is MIT-licensed and runs side-by-side with Marten (ADR-0019), enqueuing integration events to its outbox inside the write transaction and delivering them after commit. The decision and its trade-offs are recorded in [ADR-0023](./decisions/0023-wolverine-messaging-transport.md) (which supersedes [ADR-0004](./decisions/0004-asynchronous-messaging-between-services.md)).
