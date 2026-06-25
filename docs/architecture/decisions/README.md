# Architecture Decision Records (ADRs)

This directory captures the significant architectural decisions for VitalSync using lightweight **Architecture Decision Records**.

## What is an ADR?

An ADR documents a single architectural decision, its context, and its consequences. ADRs are immutable once accepted: to change a decision, add a new ADR that supersedes the old one.

## Status values

- **Proposed** — under discussion.
- **Accepted** — decided and in effect.
- **Superseded** — replaced by a later ADR (linked).

## Index

| # | Title | Status |
|---|---|---|
| [0001](./0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](./0002-use-dotnet-aspire-13-for-orchestration.md) | Use .NET Aspire 13 for orchestration | Accepted |
| [0003](./0003-bff-with-rest-and-code-first-grpc.md) | BFF with REST externally and code-first gRPC internally | Accepted |
| [0004](./0004-asynchronous-messaging-between-services.md) | Asynchronous messaging between services | Proposed |
| [0005](./0005-strongly-typed-aggregate-identifiers.md) | Strongly typed aggregate identifiers | Accepted |

## Template

```markdown
# NNNN. Title

- **Status:** Proposed | Accepted | Superseded by ADR-XXXX
- **Date:** YYYY-MM-DD

## Context
What is the issue and the forces at play?

## Decision
What is the change we are making?

## Consequences
What becomes easier or harder as a result?

## Alternatives considered
What other options were evaluated, and why were they not chosen?
```