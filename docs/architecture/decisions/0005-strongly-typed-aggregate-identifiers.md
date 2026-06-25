# 0005. Strongly typed aggregate identifiers

- **Status:** Accepted
- **Date:** 2026-06-23

## Context

Using primitive types (e.g., `Guid`) for aggregate identifiers makes it easy to accidentally pass an identifier of the wrong aggregate (for example, supplying an ingredient identifier where a recipe identifier is expected). Such mistakes should be caught as early as possible.

## Decision

Aggregate identifiers are represented by **strongly typed Value Objects**. Each aggregate has its own identifier type (for example, a distinct `RecipeId` and `IngredientId`), even when both wrap the same underlying primitive. Incorrect usage of identifiers belonging to different aggregates is detected at **compile time**.

## Consequences

- Whole classes of identifier-mix-up bugs become **compile-time errors**.
- Method signatures and domain models become more self-documenting.
- Persistence requires mapping between strongly typed identifiers and their underlying primitives (provided by a value converter in the Persistence building block).
- A small amount of boilerplate per identifier type is introduced (mitigated by shared base types in `BuildingBlocks.Domain`).

## Alternatives considered

- **Primitive identifiers (`Guid`/`int`):** simplest, but unsafe — mixing identifiers compiles and fails only at runtime, if at all.
- **A single shared id type for all aggregates:** still allows mixing identifiers of different aggregates; loses type safety.