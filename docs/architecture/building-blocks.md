# Building Blocks

The **Building Blocks** are a reusable platform of shared concepts and components that underpin the microservices **without coupling them to VitalSync**. They are designed to be reusable in future projects.

## Goals

- Provide consistent, reusable primitives across all services.
- Remain **independent of VitalSync** — no references to VitalSync-specific concepts.
- Keep the **Domain** and **Application** building blocks free of any framework or third-party dependency.

## The three packages

The platform is deliberately split into exactly **three** packages. The boundary between them is not functional (persistence vs. messaging vs. …) — it is about **purity and third-party dependencies**. See [ADR-0018](./decisions/0018-three-building-block-packages.md).

### `BuildingBlocks.Domain`

`BuildingBlocks.Domain` is the pure core of the platform. It provides the tactical Domain-Driven Design primitives that every service's domain layer builds upon: entities, the unified aggregate-root base classes (state fold, with event sourcing additive — ADR-0025), domain events, value objects, strongly typed identifiers, domain exceptions, business-rule and validation abstractions, and the `IClock` abstraction.

It is deliberately kept **pure**: it declares **no** package references and depends on nothing but the BCL. No framework, no infrastructure, no third party. This is what keeps the heart of every service clean and portable.

> Detailed reference: [BuildingBlocks.Domain](./building-blocks-domain.md).

### `BuildingBlocks.Application`

`BuildingBlocks.Application` is the framework-agnostic use-case layer. It defines the **CQRS abstractions** (commands, queries, and their handlers), the **pipeline-behavior** contract, the **dispatcher** (`ISender`) contract, and the **`Result` / `Failure`** model.

It depends **only** on `Domain`, and — like `Domain` — it references **no** third party: it holds *contracts*, not implementations. The DI-based dispatcher and the concrete behaviors that fulfill these contracts live in `Infrastructure`.

> Detailed reference: [BuildingBlocks.Application](./building-blocks-application.md).

### `BuildingBlocks.Infrastructure`

`BuildingBlocks.Infrastructure` is the single outer layer that holds **all** the reusable, framework-bound, third-party-backed implementations that are still **independent of any VitalSync domain logic**:

- the **unit of work**;
- **generic repositories** for EF Core and for the **Marten-based event store** (see [ADR-0019](./decisions/0019-event-store-technology-marten.md));
- **domain event dispatching** and the outbox-backed **Publisher** with its projection runner (see [ADR-0022](./decisions/0022-event-driven-read-models.md));
- **integration event dispatching**;
- the **Wolverine**-based messaging transport on top of **RabbitMQ** (see [ADR-0023](./decisions/0023-wolverine-messaging-transport.md));
- the DI-based CQRS **dispatcher** and **pipeline behaviors** that implement the `Application` contracts.

It depends on `Domain` and `Application`, and it is where every third-party dependency of the platform is localized. There are deliberately **no** separate `EventProcessing` or `Persistence` packages (see [ADR-0018](./decisions/0018-three-building-block-packages.md)).

> Detailed reference: [BuildingBlocks.Infrastructure](./building-blocks-infrastructure.md).

## How they depend on each other

The dependency direction follows the purity boundary:

- `Domain` sits at the root and depends on nothing.
- `Application` depends only on `Domain`.
- `Infrastructure` depends on both `Domain` and `Application`.

Nothing depends on `Infrastructure`; everything is allowed to depend on `Domain`. The simple rule that decides where a piece of code belongs: **if it is reusable and VitalSync-agnostic but needs a framework or third party, it belongs in `Infrastructure`; if it is tied to a specific service's domain logic, it belongs in that service.**

## Key design rules enforced here

- **Domain and Application have zero third-party dependencies.** `Domain` is BCL-only; `Application` holds contracts only. All framework and third-party code lives in `Infrastructure`. See [ADR-0018](./decisions/0018-three-building-block-packages.md).
- **Contracts live in the innermost layer that consumes them.** A contract's home is decided by its *consumer*, not its implementor: `IRepository<,>`, `IUnitOfWork`, the projection/publisher abstractions, and the integration-event marker all live in `Application` because only handlers and use-case orchestration consume them; `Domain` stays free of persistence, transaction, and messaging concepts. See [ADR-0024](./decisions/0024-contract-placement-innermost-consumer.md).
- **Aggregates own their domain events.** Only an aggregate can raise or remove its events; outside layers receive a **read-only** view. See [Domain model](./domain-model.md).
- **Strongly typed identifiers.** Aggregate identifiers are strongly typed Value Objects, so mixing identifiers of different aggregates fails at **compile time**. See [ADR-0005](./decisions/0005-strongly-typed-aggregate-identifiers.md).
- **CQRS by default.** Commands and queries are explicit and separated at the Application layer. Contracts and the `Result` model live in `BuildingBlocks.Application`; a **hand-rolled dispatcher** implements them in `Infrastructure`. See [ADR-0015](./decisions/0015-hand-rolled-cqrs-mediator.md).
- **Uniform failure channel.** Expected domain errors are translated to `Result.Failure` at the Application boundary; unexpected errors are handled globally. See [ADR-0017](./decisions/0017-application-error-handling-and-result.md).
- **Reliable messaging via the outbox.** Domain events are collected on save and forwarded — through the outbox-backed Publisher and the Wolverine/RabbitMQ transport in `Infrastructure` — to in-context projections and the messaging backbone. The transport leg is durable end to end (durable sending endpoint, persistent messages, durable quorum queues), so the at-least-once promise survives a broker restart and a process crash. See [ADR-0022](./decisions/0022-event-driven-read-models.md) and [ADR-0023](./decisions/0023-wolverine-messaging-transport.md).

## Testing

Each building block has a corresponding test project. See [Testing strategy](./testing-strategy.md).
