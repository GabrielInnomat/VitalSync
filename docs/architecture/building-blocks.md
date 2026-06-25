# Building Blocks

The **Building Blocks** are a reusable platform of shared concepts and components that underpin the microservices **without coupling them to VitalSync**. They are designed to be reusable in future projects.

## Goals

- Provide consistent, reusable primitives across all services.
- Remain **independent of VitalSync** — no references to VitalSync-specific concepts.
- Keep the **Domain** building block free of any infrastructure or third-party dependency.

## Packages

| Building Block                   | Responsibility                                                                                         | Depends on                            |
| -------------------------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------- |
| `BuildingBlocks.Domain`          | Entities, Aggregate Roots, Domain Events, Value Objects, strongly typed identifiers, domain exceptions | _(nothing — BCL only)_                |
| `BuildingBlocks.Common`          | Cross-cutting utilities (e.g., `Result`, clock abstraction)                                            | _(BCL only)_                          |
| `BuildingBlocks.Application`     | CQRS abstractions (commands, queries, handlers) and pipeline behaviors                                 | `Common`                              |
| `BuildingBlocks.EventProcessing` | Domain event handler/dispatcher abstractions, outbox abstractions                                      | `Domain`                              |
| `BuildingBlocks.Persistence`     | EF Core base `DbContext` (unit of work + event collection), strongly typed id value converters         | `Domain`, `EventProcessing`           |
| `BuildingBlocks.Infrastructure`  | Default implementations of cross-cutting abstractions (e.g., DI-based dispatcher)                      | `Domain`, `EventProcessing`, `Common` |

## Dependency direction

```text
        ┌──────────────────────────┐
        │   BuildingBlocks.Domain  │  (no dependencies)
        └─────────────┬────────────┘
                      │
       ┌──────────────┼───────────────┐
       │              │               │
┌──────▼──────┐ ┌─────▼─────────┐     │
│EventProcess.│ │   (Common)    │     │
└──────┬──────┘ └─────┬─────────┘     │
       │              │               │
┌──────▼──────┐ ┌─────▼─────────┐ ┌───▼───────────┐
│ Persistence │ │  Application  │ │Infrastructure │
└─────────────┘ └───────────────┘ └───────────────┘
```

The arrows point **toward dependencies**. Nothing depends on `Infrastructure`, `Persistence`, or `Application`; everything can depend on `Domain`.

## Key design rules enforced here

- **Domain has zero infrastructure dependencies.** The `BuildingBlocks.Domain` project intentionally declares no package references.
- **Aggregates own their domain events.** Only an aggregate can raise or remove its events; outside layers receive a **read-only** view. See [Domain model](./domain-model.md).
- **Strongly typed identifiers.** Aggregate identifiers are strongly typed Value Objects, so mixing identifiers of different aggregates fails at **compile time**. See [ADR-0005](./decisions/0005-strongly-typed-aggregate-identifiers.md).
- **CQRS by default.** Commands and queries are explicit and separated at the Application layer.
- **Reliable messaging via outbox.** Domain events are collected on save and can be forwarded through an outbox to the messaging backbone.

## Testing

Each building block has a corresponding test project. See [Testing strategy](./testing-strategy.md).
