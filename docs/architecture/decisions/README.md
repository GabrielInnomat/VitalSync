# Architecture Decision Records (ADRs)

This directory captures the significant architectural decisions for VitalSync using lightweight **Architecture Decision Records**.

## What is an ADR?

An ADR documents a single architectural decision, its context, and its consequences. ADRs are immutable once accepted: to change a decision, add a new ADR that supersedes the old one.

## Status values

- **Proposed** — under discussion.
- **Accepted** — decided and in effect.
- **Superseded** — replaced by a later ADR (linked).

## Amendments

Immutability protects the **decision**, not every sentence around it. An accepted ADR may
carry an **amendment** when reality forces a correction that leaves the decision intact —
an illustrative detail that was never true (a name, a spelling), or an implementation
constraint discovered by the first real consumer. Anything that changes *what was decided*
needs a superseding ADR instead.

Format (see ADR-0021, 0022, 0025, 0026, 0027): add
`- **Amended:** YYYY-MM-DD (short reason)` to the header, and put a blockquote **at the
place it corrects**, stating what the ADR said, what holds instead, and why. Never edit the
original wording away — the amendment must remain readable as a correction.

## Index

| #                                                         | Title                                                   | Status                    |
| --------------------------------------------------------- | ------------------------------------------------------- | ------------------------- |
| [0001](./0001-record-architecture-decisions.md)           | Record architecture decisions                           | Accepted                  |
| [0002](./0002-use-dotnet-aspire-13-for-orchestration.md)  | Use .NET Aspire 13 for orchestration                    | Accepted                  |
| [0003](./0003-bff-with-rest-and-code-first-grpc.md)       | BFF with REST externally and code-first gRPC internally | Accepted                  |
| [0004](./0004-asynchronous-messaging-between-services.md) | Asynchronous messaging between services                 | Superseded by ADR-0023    |
| [0005](./0005-strongly-typed-aggregate-identifiers.md)    | Strongly typed aggregate identifiers                    | Accepted                  |
| [0006](./0006-aggregate-owns-domain-events.md)            | Aggregate owns its domain events                        | Accepted                  |
| [0007](./0007-read-only-vs-managed-domain-events.md)      | Read-only vs. managed domain events                     | Accepted                  |
| [0008](./0008-entity-identity-and-equality.md)            | Entity identity and equality                            | Accepted                  |
| [0009](./0009-business-rules-and-domain-validation.md)    | Business rules and domain validation                    | Accepted                  |
| [0010](./0010-aggregate-state-object.md)                  | Aggregate state object                                  | Accepted                  |
| [0011](./0011-unified-aggregate-for-es-and-ef.md)         | Unified aggregate for event sourcing and EF Core        | Superseded by ADR-0012    |
| [0012](./0012-optional-event-sourcing-aggregate.md)       | Optional event sourcing via a split aggregate hierarchy | Superseded by ADR-0025    |
| [0013](./0013-xml-documentation-conventions.md)           | XML documentation conventions for Building Blocks       | Accepted                  |
| [0014](./0014-replace-fluentassertions-with-xunit-asserts.md) | Replace FluentAssertions with standard xUnit asserts | Accepted                  |
| [0015](./0015-hand-rolled-cqrs-mediator.md)               | Hand-rolled CQRS mediator instead of MediatR            | Accepted                  |
| [0016](./0016-remove-common-result-in-application.md)     | Remove BuildingBlocks.Common; Result lives in Application | Accepted                |
| [0017](./0017-application-error-handling-and-result.md)   | Application error handling: domain exceptions → Result  | Accepted                  |
| [0018](./0018-three-building-block-packages.md)           | Three building block packages: Domain, Application, Infrastructure | Accepted        |
| [0019](./0019-event-store-technology-marten.md)           | Marten on PostgreSQL as the event store                 | Accepted                  |
| [0020](./0020-postgresql-for-state-stored-contexts.md)    | PostgreSQL for state-stored contexts; database per bounded context | Accepted        |
| [0021](./0021-write-read-database-pair-per-context.md)    | Write/read database pair per bounded context            | Accepted                  |
| [0022](./0022-event-driven-read-models.md)               | Event-driven read models via an outbox-backed publisher | Accepted                  |
| [0023](./0023-wolverine-messaging-transport.md)           | Wolverine as the messaging transport (replaces MassTransit) | Accepted (supersedes ADR-0004) |
| [0024](./0024-contract-placement-innermost-consumer.md)   | Contracts live in the innermost layer that consumes them | Accepted                  |
| [0025](./0025-unified-state-fold-aggregate-model.md)      | Unified state-fold aggregate model with additive event sourcing | Accepted (supersedes ADR-0012) |
| [0026](./0026-single-repository-contract.md)              | Single repository contract: add and get, no delete      | Accepted                  |
| [0027](./0027-building-blocks-own-wolverine-wiring.md)    | Building Blocks own the persistence and Wolverine wiring | Accepted                 |

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
