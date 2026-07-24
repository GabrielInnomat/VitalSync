# 0016. Remove BuildingBlocks.Common; Result lives in BuildingBlocks.Application

- **Status:** Accepted
- **Date:** 2026-07-24

## Context

The documented Building Blocks platform included a `BuildingBlocks.Common`
project described as holding "cross-cutting utilities (e.g., `Result`)". In
practice, the only concept it was slated to carry was the `Result` / `Result<T>`
type used by the Application layer's CQRS contracts.

A dedicated project for a single cross-cutting type adds a package, a dependency
edge, and documentation surface without earning its keep. The `Result` type is
used **by and for** the Application layer's command/query return conventions, so
it has a natural home there.

Removing a documented Building Block changes the platform's structure and
dependency graph, so it warrants an ADR.

## Decision

- **Remove `BuildingBlocks.Common`** entirely, along with all references to it in
  the documentation.
- **Move `Result` / `Result<T>`** into `BuildingBlocks.Application`.
- `BuildingBlocks.Application` depends on **`BuildingBlocks.Domain`** (it needs the
  domain exception types for the exception-to-`Result` translation behaviour; see
  [ADR-0017](./0017-application-error-handling-and-result.md)). No `Common`
  dependency remains.

This also resolves a prior inconsistency in `docs/architecture/building-blocks.md`,
where the dependency table listed Application's dependency as `Common` while the
diagram implied a dependency on `Domain`.

## Consequences

- **Easier:** One fewer project to build, reference, and document. `Result` sits
  next to the CQRS contracts that define and consume it, keeping the return
  conventions cohesive.
- **Harder:** Any future genuinely cross-cutting utility that is *not*
  Application-specific has no `Common` home and must be placed deliberately (in the
  most appropriate existing block, or a new one justified by its own ADR).

## Alternatives considered

- **Keep `Common` for `Result`** — rejected: a whole project for one type adds
  structure without value.
- **Put `Result` in `BuildingBlocks.Domain`** — rejected: `Result` is an
  application-boundary concept (command/query outcomes), not a tactical DDD
  primitive; the Domain intentionally throws rather than returns results
  (see [ADR-0009](./0009-business-rules-and-domain-validation.md)).
- **No `Result` type (raw values + exceptions everywhere)** — rejected: commands
  and queries return a uniform success/failure result, which the frontend and BFF
  rely on for consistent handling.
