# 0001. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-06-23

## Context

VitalSync mandates a number of architectural principles and will evolve iteratively. We need a durable, lightweight way to capture *why* decisions were made, so future contributors understand the reasoning and can challenge or supersede decisions deliberately.

## Decision

We will use **Architecture Decision Records (ADRs)**, stored as Markdown files in `docs/architecture/decisions/`, numbered sequentially. Each significant architectural decision gets its own record. Accepted ADRs are immutable; changes are made by adding a superseding ADR.

## Consequences

- Decisions and their rationale are documented and discoverable.
- New contributors can ramp up on the "why," not just the "what."
- A small amount of process overhead is introduced for each decision.

## Alternatives considered

- **A single architecture document:** harder to track the evolution of individual decisions and their rationale over time.
- **No formal record:** rationale would be lost, making it harder to challenge decisions safely.