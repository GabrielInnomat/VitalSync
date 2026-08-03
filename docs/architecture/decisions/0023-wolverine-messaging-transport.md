# 0023. Wolverine as the messaging transport (replaces MassTransit)

- **Status:** Accepted
- **Date:** 2026-07-27
- **Supersedes:** [ADR-0004](./0004-asynchronous-messaging-between-services.md)
- **Amended:** 2026-07-31 (broker topology for integration events — see the note below)
- **Amended:** 2026-08-01 (subscribing half — see the note below)
- **Amended:** 2026-08-03 (topic attribute owned by Building Blocks — see the note below)

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

> **Broker topology (amendment 2026-07-31).** Connecting the transport does not
> move anything: Wolverine routes a message only where a **routing rule** sends
> it, and `PublishAsync` **silently discards** a message with no route. Until this
> amendment no such rule existed, so integration events never reached the broker.
> The topology is therefore fixed as follows.
>
> - **One topic exchange for the whole platform**, `vitalsync.integration-events`,
>   provisioned automatically (`AutoProvision`). Consumers bind their own queue
>   with a topic pattern (`nutrition.*`), so adding a subscriber never touches the
>   publishing service.
> - **The rule matches the `IIntegrationEvent` marker**, never all messages. This
>   is load-bearing, not cosmetic: `DomainEventEnvelope` — which carries a
>   context's raw domain events through the local outbox queue — does not
>   implement the marker and therefore **cannot** be routed onto the broker.
>   Publishing domain events across a context boundary would break ADR-0022's rule
>   that integration events are the only cross-context signal. Pinned by
>   `IntegrationEventRoutingTests`.
> - **Every integration event carries an explicit
>   `[Topic("<context>.<event>")]`** in kebab-case (`nutrition.recipe-created`).
>   The routing key is thereby part of the published contract instead of being
>   derived from the CLR namespace, where a rename would silently break consumer
>   bindings.
> - **Publishing side only.** Queue declaration, binding, and listening belong to
>   the **subscribing** service and are deliberately not wired by Building Blocks;
>   the consumer half is added when the first real subscriber exists.
>   _Superseded by the subscribing-half amendment below._

> **Subscribing half (amendment 2026-08-01).** The first real subscriber now exists
> (stage 3 of the walking skeleton), and wiring it in the service host was tried
> first and rejected. Building Blocks therefore owns **both** halves.
>
> - **`BuildingBlocksOptions.SubscribeToIntegrationEvents(queueName, consumerAssembly, topicPatterns)`**
>   is the mirror image of `UseWolverineMessaging`. It declares the service's queue,
>   binds it to `vitalsync.integration-events` with the given patterns, enables the
>   **durable inbox**, and adds the consumer assembly to Wolverine's handler
>   discovery. The subscribing host stays at a bare `UseWolverine()` (ADR-0027).
> - **The four parts are one call because each fails silently alone.** An unbound
>   queue never fills. A bound queue whose consumers were never discovered reports
>   "no handler" once, marks the envelope handled, and drops it — no retry, no dead
>   letter. Measured, not reasoned: with the wiring in the host, four integration
>   events were lost this way before the cause was found in a log line.
> - **The consumer assembly is explicit, never the assemblies from `AddHandlersFrom`.**
>   Wolverine discovers handlers by naming convention, so a CQRS handler such as
>   `CreateRecipeHandler` would be picked up as a Wolverine message handler for
>   `CreateRecipe` and dispatched outside the `ISender` pipeline. Pass the service's
>   Infrastructure assembly; keep its Application assembly out.
> - **A subscription without `UseWolverineMessaging` throws at composition time.**
>   There is nothing to listen on, and the silent version of that mistake is
>   indistinguishable from an upstream context that has not published yet.
> - **`ISender` is opted into service location** by the domain-event routing, because
>   Wolverine cannot construct it (it takes an `IServiceProvider`) and otherwise
>   refuses to generate **any** handler that dispatches a command — which is every
>   integration-event consumer, per the scope note below.
> - **The exchange name stays internal to Building Blocks.** A subscriber that had to
>   restate it could bind to the wrong one, and nothing would report it.
> - **One queue per service.** A second `SubscribeToIntegrationEvents` call throws; a
>   service that consumes several contexts binds several patterns to its one queue.
> - **Consequence to know:** a pattern like `sample.*` also matches the subscriber's
>   **own** published events, which are then delivered back to it. Harmless when no
>   handler exists, but a context that both publishes and consumes under one prefix
>   must expect its own messages.
> - **Where a poison message ends up.** A consumer that keeps throwing is retried three
>   times with a growing cooldown and the message is then moved to Wolverine's
>   `wolverine-dead-letter-queue` **on the broker** — not to the `wolverine_dead_letters`
>   table in the context's write database, which exists but stays empty. Operationally
>   this is the one place to look; pinned by `DeadLetterTests`.

> **Topic attribute owned by Building Blocks (amendment 2026-08-03).** The routing
> key moves from Wolverine's `[Topic]` to a Building Blocks attribute,
> **`[IntegrationEventTopic("<context>.<event>")]`** in `BuildingBlocks.Application`,
> next to the `IIntegrationEvent` marker it belongs to.
>
> - **Why.** An integration event is a published contract, and contract assemblies
>   were referencing WolverineFx for a single attribute — the transport was leaking
>   into the contract layer that this ADR's own placement rule keeps
>   framework-agnostic. The subscribe half was already wrapped
>   (`SubscribeToIntegrationEvents`); this closes the publish half.
> - **How.** `UseWolverineMessaging` routes via
>   `PublishMessagesToRabbitMqExchange<IIntegrationEvent>` with a topic source that
>   reads the attribute from the event type. The routing target and the
>   marker-not-all-messages rule are unchanged.
> - **Fail fast, twice.** The attribute validates its argument at construction:
>   exactly `<context>.<event>`, both segments lower-case kebab-case — a third
>   segment would silently escape one-word patterns like `nutrition.*`, because an
>   AMQP `*` matches exactly one word. And publishing an event **without** the
>   attribute now throws with the event's name and the fix, instead of Wolverine
>   silently publishing under a CLR-derived key that no consumer has bound
>   (WS-05's publish half). Pinned by `IntegrationEventRoutingTests`.
> - **Consequence:** contract assemblies reference only `BuildingBlocks.Application`.
>   Wolverine's own `[Topic]` is no longer read on integration events and must not
>   be used.
>
> **Codegen dependency.** Wolverine 6 no longer ships the Roslyn compiler in its
> core package, and its default `TypeLoadMode` compiles handler code at runtime.
> `BuildingBlocks.Infrastructure` therefore references
> `WolverineFx.RuntimeCompilation`, which self-activates, so hosts still configure
> nothing (ADR-0027). Pre-generating code (`TypeLoadMode.Static`) to drop Roslyn
> from production images is an additive optimisation for a later ADR.

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
