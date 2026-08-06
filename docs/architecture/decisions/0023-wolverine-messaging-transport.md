# 0023. Wolverine as the messaging transport (replaces MassTransit)

- **Status:** Accepted
- **Date:** 2026-07-27
- **Supersedes:** [ADR-0004](./0004-asynchronous-messaging-between-services.md)
- **Amended:** 2026-07-31 (broker topology for integration events — see the note below)
- **Amended:** 2026-08-01 (subscribing half — see the note below)
- **Amended:** 2026-08-03 (topic attribute owned by Building Blocks — see the note below)
- **Amended:** 2026-08-04 (persistent delivery — see the note below)
- **Amended:** 2026-08-06 (the inbox idempotency window — see the note below)
- **Amended:** 2026-08-06 (a mapper without a transport is a start-up error — see the note below)

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

> **Persistent delivery (amendment 2026-08-04).** The topology above was correct but
> **not durable**: everything it declared survived only as long as the broker process
> and the sending process did. Three defaults were left at their Wolverine values, and
> each of them loses messages on its own.
>
> - **The sending endpoint is durable** (`UseDurableOutbox()` on the publish rule).
>   Without it the endpoint stays at Wolverine's default `BufferedInMemory`, and that
>   single fact costs twice. Wolverine's RabbitMQ sender derives the AMQP flag from the
>   endpoint mode, so a buffered endpoint publishes with `delivery_mode: 1` and a broker
>   restart drops the message; and buffered means the envelope is **never written to
>   `wolverine_outgoing_envelopes`**, so a process crash between the commit and the
>   broker's acknowledgement drops it too. The outbox promise of
>   [ADR-0022](./0022-event-driven-read-models.md) therefore held only up to the moment
>   of handover — precisely the gap it was introduced to close.
> - **The exchange and the subscriber queue are declared durable.** Both were durable by
>   default already; declaring them explicitly makes the guarantee part of the topology
>   rather than a value someone may change.
> - **Queues are quorum queues, transport-wide** (`UseQuorumQueues()`), not per queue.
>   Classic queues are unreplicated. Configuring the setting on the transport rather
>   than on each declaration is what also covers the queues Building Blocks never names
>   itself — above all Wolverine's `wolverine-dead-letter-queue`, which the subscribing
>   amendment calls "operationally the one place to look" and which would otherwise be
>   the least durable queue in the system. Wolverine's own system queues stay classic,
>   which is required and correct.
> - **Messaging without a persistence strategy throws at composition time.** A durable
>   sending endpoint needs a message store, and `UseWolverineMessaging` alone does not
>   provide one. Wolverine's behaviour in that situation is to degrade quietly, which
>   would reproduce this amendment's own bug in a new place: a host that looks durable
>   and is not. `AddBuildingBlocks` therefore fails when a broker URI was given without
>   `UseEfCorePersistence` or `UseMartenEventSourcing`. The check runs after the whole
>   options lambda, so the order of the two calls does not matter.
> - **Consequence to know:** the queue type is part of the declaration and cannot be
>   changed on an existing queue. A broker that still carries a classic queue of the
>   same name from an earlier run makes `AutoProvision` fail; the queue has to be
>   deleted. No environment is affected today, but a long-lived broker will be.
> - **Pinned by** `IntegrationEventDurabilityTests` (a published event arrives at a real
>   broker with `Persistent == true`; the compiled endpoint is `Durable`; the subscriber
>   queue and the dead-letter queue are quorum and durable) and, without Docker, by
>   `WolverineExtensionTests`.

> **Context identity and exchange ownership (amendment 2026-08-05).** A host now names
> its own bounded context, and the exchange name moves out of Building Blocks:
> `UseWolverineMessaging(rabbitMqUri, exchangeName, contextName)`. All three are
> transport coordinates, so they are given together and none can be set without the
> others.
>
> - **This revises the 2026-08-01 point "the exchange name stays internal to Building
>   Blocks".** That reasoning was right about the risk and wrong about who bears it:
>   `vitalsync.integration-events` was the single VitalSync string inside a package
>   whose whole purpose ([ADR-0018](./0018-three-building-block-packages.md)) is to be
>   reusable and product-independent. The promise wins; the risk is mitigated instead
>   of avoided — VitalSync defines the name **once** in
>   `VitalSync.ServiceDefaults` (`VitalSyncMessaging.IntegrationEventExchangeName`) and
>   every host passes that constant, never a literal.
> - **The context name is mandatory** as soon as messaging is selected, and it is
>   validated as a single lower-case kebab-case word. A value containing a dot is
>   rejected with a hint, because it is almost always the exchange name in the wrong
>   position — the two arguments are adjacent strings.
> - **Publishing under a foreign context throws** (`IntegrationEventTopic.For(type,
>   contextName)`). The first segment of a routing key names the owner of the contract
>   and consumers bind to it, so publishing `fitness.…` from Nutrition makes one service
>   impersonate another. Before this, nothing noticed.
> - **A context does not consume its own integration events.** This closes the
>   2026-08-01 "consequence to know" (a pattern like `sample.*` also matches the
>   subscriber's own events) with a rule instead of a warning, in two layers:
>   - Every published event carries the header `buildingblocks.source-context`, and a
>     consumer-side Wolverine middleware stops any integration event whose source is the
>     consuming context itself.
>   - At start-up, a handler for an event of the **own** context fails the host — it
>     would be unreachable by the rule above, and an unreachable handler is exactly the
>     silent failure this ADR keeps eliminating.
>
>   The two together make the suppression **provably lossless**: it can never skip a
>   handler that was allowed to exist. Read the middleware only in that light; on its
>   own it would look like precisely the quiet discard we fight elsewhere.
> - **Handler ⇒ pattern is checked at start-up, and it is an error.** For every
>   integration event handled by the declared consumer assembly, at least one bound
>   topic pattern must match its topic; otherwise the host fails, naming the type, the
>   topic and the bound patterns. Direction matters: "does anyone publish on my pattern?"
>   is not locally decidable, but "do I receive what I have a handler for?" is — and it
>   is the direction in which a typo actually costs messages. The reverse (a pattern with
>   no matching contract) stays deliberately unchecked: binding ahead of an upstream
>   context that does not exist yet is legitimate.
> - **Consequence for the samples.** Both walking-skeleton slices published under one
>   prefix `sample.` — two bounded contexts sharing one identity, which the rules above
>   make impossible. They are now `sample-state-stored` and `sample-event-sourced`, and
>   the event-sourced slice binds `sample-state-stored.*`.
> - **Pinned by** `IntegrationEventContextTests` and `TopicPatternMatcherTests` (without
>   Docker) and `IntegrationEventSubscriptionValidationTests` (with a real broker: both
>   start-up refusals, plus an event stopped by its source header while an event from a
>   foreign context reaches the handler).


> **Scope note — Wolverine is the transport, not the mediator.** Wolverine is
> adopted **only** as the inter-service messaging transport. It is **not** used
> as the in-process CQRS mediator: the hand-rolled `ISender` dispatcher of
> [ADR-0015](./0015-hand-rolled-cqrs-mediator.md) stays in place. Where an
> incoming Wolverine message must trigger domain work, its handler is a **thin
> adapter** that calls `ISender`, so the `Result` model, exception-to-Result
> translation (ADR-0017), and pipeline-behavior ordering remain authoritative
> and the framework-agnostic core stays decoupled from the transport.

> **The inbox idempotency window is a decision, not a default (amendment 2026-08-06).**
> Delivery is at-least-once, so TODO-14 asked for consumer-side bookkeeping over processed
> `EventId`s. Measuring first showed that half of it already existed — and that the other
> half failed in the way this repository keeps finding.
>
> - **The durable inbox already deduplicates.** `SubscribeToIntegrationEvents` listens with
>   `UseDurableInbox()`, so each incoming envelope is stored in
>   `wolverine_incoming_envelopes` whose primary key is the envelope id. A second `INSERT`
>   of the same id raises PostgreSQL `23505`, which Wolverine turns into a
>   `DuplicateIncomingEnvelopeException` and answers by acknowledging the message **without
>   invoking a handler**. The id survives the wire because the RabbitMQ envelope mapper
>   writes it as the AMQP `MessageId` and reads it back on arrival. A nack, a requeue, a
>   consumer crash before the ack, a broker reconnect, and the sender's own durable-outbox
>   retry are therefore all covered, and always were.
> - **But the guarantee expired after five minutes.** Wolverine deletes handled inbox rows
>   after `DurabilitySettings.KeepAfterMessageHandling`, whose default is `5.Minutes()` —
>   rows its own source comments call *"records to use in idempotency checking"*. The
>   system's idempotency promise thus rested on a retention period nobody here had chosen:
>   the same shape as ADR-0034 (`IsEmpty` in the event stream) and ADR-0035 (derived field
>   names), a permanent decision inherited from a default.
> - **The window is now 7 days**, applied by `ApplyBuildingBlockIdempotencyWindow` exactly
>   when a persistence strategy was selected — without a message store there are no inbox
>   rows to keep. Seven days covers a weekend plus the time an operator needs to replay a
>   message out of the dead-letter queue, which is the realistic case: the same envelope id
>   arriving hours later, long after the row would have been swept. A test pins that the
>   value is provably **not** the framework default, so the guarantee cannot quietly revert
>   when Wolverine changes its own.
> - **What stays uncovered, deliberately.** A republication under a *new* envelope id — an
>   outbox replay, an operational re-send, a future event replay — passes the inbox
>   untouched, because the inbox key is transport identity. The business key for that case
>   is `IIntegrationEvent.EventId` ([ADR-0029](./0029-event-identity-placement.md)), and a
>   dedup table keyed by it is **deferred** until the replay question is settled
>   (TODO-14 part B, hanging off TODO-21): without a replay the remaining gap is too narrow
>   to justify a table, two persistence implementations, a start-up check and a retention
>   strategy. Until then, **shared identity is the sanctioned idempotency route** for a
>   consumer deriving its own aggregate, as recorded in `communication.md`.

> **A mapper without a transport is a start-up error (amendment 2026-08-06).** The guard
> rails above all protect a *configured* transport. The remaining hole was the missing one:
> a host that registers an `IIntegrationEventMapper` and never calls
> `UseWolverineMessaging`. Every event the mapper produces is then handed to
> `NullIntegrationEventSink`, logged as a warning and dropped, while the commit reports
> success — the exact failure shape this ADR already rejects for a message with no route.
> `IntegrationEventMapperCheck` now fails the host at start, naming the mappers.
>
> The check asks about the **effect**, not about the selection: it fires when mappers are
> registered *and* the resolved `IIntegrationEventSinkFactory` is still the null one. A
> host that supplies its own sink factory therefore passes, which is what makes the
> guard compatible with the delivery tests.
>
> There is deliberately **no `UseNoMessaging()`** as a counterpart to `UseNoPersistence()`.
> That escape hatch was in the original proposal and is refused for the reason ADR-0027's
> 2026-08-05 amendment gives: an opt-out restores exactly the silence the check exists to
> remove, and the host reaching for it is the one already in trouble. The asymmetry to
> `UseNoPersistence()` is real but principled — "this host commits nothing" is an intent
> that cannot be read off the code, whereas "this host publishes nothing" is simply the
> absence of a mapper.

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
