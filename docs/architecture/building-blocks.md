# Building Blocks

The **Building Blocks** are a reusable platform of shared concepts and components that underpin the microservices **without coupling them to VitalSync**. They are designed to be reusable in future projects.

## Goals

- Provide consistent, reusable primitives across all services.
- Remain **independent of VitalSync** — no references to VitalSync-specific concepts.
- Keep the **Domain** and **Application** building blocks free of any framework or third-party dependency.

## The three packages

The platform is deliberately split into exactly **three** packages. The boundary between them is not functional (persistence vs. messaging vs. …) — it is about **purity and third-party dependencies**. See [ADR-0018](./decisions/0018-three-building-block-packages.md).

### `BuildingBlocks.Domain`

`BuildingBlocks.Domain` is the pure core of the platform. It provides the tactical Domain-Driven Design primitives that every service's domain layer builds upon: entities, the two aggregate-root bases (state-stored and event-sourced), domain events, value objects, strongly typed identifiers, domain exceptions, the business-rule and validation abstractions, and the `IClock` abstraction over time.

It is deliberately kept **pure**: it declares **no** package references and depends on nothing but the BCL. No framework, no infrastructure, no third party. This is what keeps the heart of every service testable in isolation and reusable across projects. See the [Domain reference](./building-blocks-domain.md).

### `BuildingBlocks.Application`

`BuildingBlocks.Application` is the framework-agnostic use-case layer. It defines the **CQRS abstractions** (commands, queries, and their handlers), the **pipeline-behavior** contract, the **dispatcher** (`ISender`) contract, and the shared **`Result` / `Failure`** model that every service returns.

It depends **only** on `Domain`, and — like `Domain` — it references **no** third party: it holds *contracts*, not implementations. The DI-based dispatcher and the concrete behaviors that fulfil those contracts live in `Infrastructure`, so this layer stays clean, reusable, and easy to reason about. See the [Application reference](./building-blocks-application.md) for the full CQRS contract catalog, return conventions, and `Failure` model.

### `BuildingBlocks.Infrastructure`

`BuildingBlocks.Infrastructure` is the single outer layer that holds **all** the reusable, framework-bound, third-party-backed implementations that are still **independent of any VitalSync domain logic**. Everything that needs a framework or an external library lives here, including:

- the **unit of work**;
- **generic repositories** for EF Core and for the (still TBD) event-store tool;
- **domain event dispatching**;
- **integration event dispatching**;
- the **RabbitMQ** wrapper / messaging transport;
- the DI-based CQRS **dispatcher** and **pipeline behaviors** that implement the `Application` contracts.

It depends on `Domain` and `Application`, and it is where every third-party dependency of the platform is localized. There are deliberately **no** separate `EventProcessing` or `Persistence` packages — persistence, event processing, and messaging are simply *capabilities inside* `Infrastructure`.

> A detailed reference for `BuildingBlocks.Infrastructure` is still to be written.

## How they depend on each other

The dependency direction follows the purity boundary:

- `Domain` sits at the root and depends on nothing.
- `Application` depends only on `Domain`.
- `Infrastructure` depends on both `Domain` and `Application`.

Nothing depends on `Infrastructure`; everything is allowed to depend on `Domain`. The simple rule that decides where a piece of code belongs: **if it is reusable and VitalSync-agnostic but needs a framework or third party, it belongs in `Infrastructure`; if it is pure, it belongs in `Domain` or `Application`; and if it is tied to a specific service's domain logic, it does not belong in the Building Blocks at all — it lives in that service.**

## Key design rules enforced here

- **Domain and Application have zero third-party dependencies.** `Domain` is BCL-only; `Application` holds contracts only. All framework and third-party code lives in `Infrastructure`. See [ADR-0018](./decisions/0018-three-building-block-packages.md).
- **Aggregates own their domain events.** Only an aggregate can raise or remove its events; outside layers receive a **read-only** view. See [Domain model](./domain-model.md).
- **Strongly typed identifiers.** Aggregate identifiers are strongly typed Value Objects, so mixing identifiers of different aggregates fails at **compile time**. See [ADR-0005](./decisions/0005-strongly-typed-aggregate-identifiers.md).
- **CQRS by default.** Commands and queries are explicit and separated at the Application layer. Contracts and the `Result` model live in `BuildingBlocks.Application`; a **hand-rolled dispatcher** implements them in `BuildingBlocks.Infrastructure`. See the [Application reference](./building-blocks-application.md) and [ADR-0015](./decisions/0015-hand-rolled-cqrs-mediator.md).
- **Uniform failure channel.** Expected domain errors are translated to `Result.Failure` at the Application boundary; unexpected errors are handled globally. See [ADR-0017](./decisions/0017-application-error-handling-and-result.md).
- **Reliable messaging via the outbox.** Domain events are collected on save and forwarded — through the dispatching and RabbitMQ components in `Infrastructure` — to the messaging backbone. See [Communication](./communication.md).

## Testing

Each building block has a corresponding test project. See [Testing strategy](./testing-strategy.md).
