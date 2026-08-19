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

See [Domain model](./domain-model.md) for domain events and [Thessera](./thessera.md) for the outbox/dispatch abstractions.

### Consumer idempotency

Delivery is **at-least-once**, so a consumer must tolerate seeing the same integration event twice. Two mechanisms carry that today, and it is worth knowing which one covers what.

**Wolverine's durable inbox covers the transport.** Every subscription listens with `UseDurableInbox()`, which stores each incoming envelope in `wolverine_incoming_envelopes` under a primary key on the envelope id. A second arrival of the same id violates that key, and Wolverine acknowledges the message without running any handler. The id survives the wire as the AMQP `MessageId`, so a nack, a requeue, a consumer crash before the ack, a broker reconnect and a sender-side outbox retry are all deduplicated for free.

That protection is time-boxed: handled rows are deleted after `DurabilitySettings.KeepAfterMessageHandling`. Thessera sets that window to **7 days** rather than leaving Wolverine's 5-minute default in place — long enough to cover a weekend plus the time an operator needs to replay a message out of the dead-letter queue. The window is a decision, not a default, and a test pins that it differs from the framework's.

**Shared identity covers the business case, and it is the sanctioned pattern.** A consumer that derives its own aggregate from a foreign event should adopt the foreign identity, as the walking skeleton's mirror does: `MirrorWidgetHandler` builds its `GadgetId` from the incoming `WidgetId` and returns success when the aggregate already exists. Re-processing then writes the same row twice instead of creating a second aggregate.

What is **not** covered is a republication under a *new* transport identity — an outbox replay or an operational re-send. That case does not arise in this system: ADR-0036 rebuilds a state-stored read model from the current aggregate state rather than replaying events, and the rebuild never reaches `DomainEventPublisher`. A dedup table keyed by `IIntegrationEvent.EventId` (ADR-0029) is therefore **not built**; the id stays stable per event so the option survives should a context ever switch to event sourcing and replay a stream onto the broker. Until then, do not write a consumer that depends on being called exactly once.

### Broker topology

All integration events are published to a single topic exchange, which Wolverine provisions automatically. **The exchange name belongs to the product, not to Thessera** (ADR-0023 amendment 2026-08-05): VitalSync defines it once as `VitalSyncMessaging.IntegrationEventExchangeName` (`vitalsync.integration-events`) in `VitalSync.ServiceDefaults`, and every host passes that constant to `UseWolverineMessaging` — never a literal, because a typo in one host binds it to an exchange nobody else uses and no host can notice that locally. The routing key of an event is its **`[IntegrationEventTopic("<context>.<event>")]`** attribute in kebab-case — a Thessera attribute living in `GaWeCodes.Thessera.Application` next to `IIntegrationEvent`, so contract assemblies stay free of any transport dependency (ADR-0023 amendment 2026-08-03):

```csharp
[IntegrationEventTopic("nutrition.recipe-created")]
public sealed record RecipeCreated(Guid RecipeId, Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
```

The attribute is **mandatory** on every integration event. It makes the routing key part of the published contract instead of deriving it from the CLR namespace, where a rename would silently break consumer bindings. Both halves fail fast: the attribute rejects anything but two kebab-case segments at construction, and publishing an event without the attribute throws instead of silently publishing under a key no consumer has bound.

Consumers own their side of the topology in the sense that they choose their queue name and topic patterns; Thessera wires **both** halves, so a subscribing host adds nothing of its own: `SubscribeToIntegrationEvents(queueName, consumerAssembly, patterns)` declares the queue, binds it to the exchange, enables the durable inbox and registers the consumer assembly (ADR-0023 amendment 2026-08-01). Adding a subscriber never changes the publishing service.

**A service knows which context it is** (ADR-0023 amendment 2026-08-05). `UseWolverineMessaging(rabbitMqUri, exchangeName, contextName)` takes the three transport coordinates together, and the context name is a single lower-case kebab-case word — the first segment of every routing key the service publishes. Three rules follow, and each of them replaces a failure that used to be silent:

- **Publishing under a foreign context throws.** The first segment names the owner of the contract and consumers bind to it, so a Nutrition service publishing `fitness.…` impersonates another service.
- **A context does not consume its own integration events.** Every published event carries the header `thessera.source-context`; a consumer-side middleware discards an integration event whose source is the consuming context itself. This is what a pattern like `nutrition.*` in the Nutrition service used to do — deliver its own events back to it.
- **A handler must be reachable.** At start-up, every integration event handled by the declared consumer assembly must be matched by at least one bound topic pattern, and none of them may belong to the service's own context; otherwise the host fails, naming the type, its topic and the bound patterns. Together with the previous rule this makes the suppression **provably lossless** — it can never skip a handler that was allowed to exist. The reverse check (a bound pattern with no matching contract) is deliberately omitted: binding ahead of an upstream context that does not exist yet is legitimate.

**Delivery is durable end to end** (ADR-0023 amendment 2026-08-04). The exchange and every queue are declared **durable**, queues are **quorum** queues transport-wide — including Wolverine's `wolverine-dead-letter-queue` — and the sending endpoint to the exchange is **durable**, so an outgoing event is written to `wolverine_outgoing_envelopes` before it is sent and reaches the broker with `delivery_mode: 2`. Neither a broker restart nor a process crash between commit and acknowledgement loses an integration event. Because the durable sending endpoint needs a message store, `UseWolverineMessaging` without `UseEfCorePersistence` or `UseMartenEventSourcing` fails at composition time rather than degrading to a host that only looks durable.

Domain events never reach the broker. The publishing rule matches the `IIntegrationEvent` contract, and the `DomainEventEnvelope` that carries domain events through a service's local outbox queue does not implement it — integration events remain the only cross-context signal (ADR-0022).

## Messaging platform

The messaging platform is **RabbitMQ**, accessed through the **Wolverine** abstraction (publish/subscribe, transactional outbox, retries, dead-lettering). Wolverine is MIT-licensed and runs side-by-side with Marten (ADR-0019), enqueuing integration events to its outbox inside the write transaction and delivering them after commit. The decision and its trade-offs are recorded in [ADR-0023](./decisions/0023-wolverine-messaging-transport.md) (which supersedes [ADR-0004](./decisions/0004-asynchronous-messaging-between-services.md)).
