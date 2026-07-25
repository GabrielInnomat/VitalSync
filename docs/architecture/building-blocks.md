# Building Blocks

The **Building Blocks** are a reusable platform of shared concepts and components that underpin the microservices **without coupling them to VitalSync**. They are designed to be reusable in future projects.

## Goals

- Provide consistent, reusable primitives across all services.
- Remain **independent of VitalSync** — no references to VitalSync-specific concepts.
- Keep the **Domain** building block free of any infrastructure or third-party dependency.

## Packages

## Key design rules enforced here

- **Domain has zero infrastructure dependencies.** The `BuildingBlocks.Domain` project intentionally declares no package references.
- **Aggregates own their domain events.** Only an aggregate can raise or remove its events; outside layers receive a **read-only** view. See [Domain model](./domain-model.md).
- **Strongly typed identifiers.** Aggregate identifiers are strongly typed Value Objects, so mixing identifiers of different aggregates fails at **compile time**. See [ADR-0005](./decisions/0005-strongly-typed-aggregate-identifiers.md).
- **CQRS by default.** Commands and queries are explicit and separated at the Application layer. Contracts and the `Result` model live in `BuildingBlocks.Application`; a **hand-rolled dispatcher** is used instead of a third-party mediator. See [ADR-0015](./decisions/0015-hand-rolled-cqrs-mediator.md).
- **Uniform failure channel.** Expected domain errors are translated to `Result.Failure` at the Application boundary; unexpected errors are handled globally. See [ADR-0017](./decisions/0017-application-error-handling-and-result.md).
- **Reliable messaging via outbox.** Domain events are collected on save and can be forwarded through an outbox to the messaging backbone.

## Testing

Each building block has a corresponding test project. See [Testing strategy](./testing-strategy.md).
