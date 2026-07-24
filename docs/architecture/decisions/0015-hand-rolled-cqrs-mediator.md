# 0015. Hand-rolled CQRS mediator instead of MediatR

- **Status:** Accepted
- **Date:** 2026-07-24

## Context

`BuildingBlocks.Application` provides the CQRS abstractions (commands, queries,
handlers) and pipeline behaviors used by every microservice. A common way to wire
commands/queries to their handlers — with a pipeline of cross-cutting behaviors —
is the **mediator** pattern, most popularly provided by the **MediatR** library.

MediatR has moved to a **commercial license** for its current versions. VitalSync
has already established a principle of avoiding paid or restrictively licensed
dependencies for fundamental, cross-cutting concerns (see
[ADR-0014](./0014-replace-fluentassertions-with-xunit-asserts.md), which removed
FluentAssertions for the same reason).

In addition, the Building Blocks are required to remain **framework-agnostic and
reusable in future projects** — taking a dependency on any third-party mediator
(MediatR or an alternative) couples this layer to that library's licensing,
lifecycle, and opinions.

Because the mediator is a documented, cross-cutting standard for the repository,
this decision affects architecture and therefore warrants an ADR.

## Decision

Do **not** depend on MediatR or any third-party mediator library. Instead,
implement a small, **hand-rolled CQRS mediator** owned by the Building Blocks:

- The **contracts** live in `BuildingBlocks.Application` and depend only on
  `BuildingBlocks.Domain`:
  - `ICommand`, `ICommand<TResult>` — intent; return `Result` / `Result<TResult>`.
  - `IQuery<TResult>` — read-only; returns `Result<TResult>`.
  - `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>`,
    `IQueryHandler<TQuery, TResult>`.
  - `IPipelineBehavior<TRequest, TResponse>` — cross-cutting behavior contract.
  - `ISender` — the dispatch entry point (contract only).
- The **default dispatcher implementation** lives in
  `BuildingBlocks.Infrastructure` (DI-based), resolving the handler and the
  ordered pipeline behaviors from the container.
- All handler and dispatch methods are **asynchronous** and accept a
  `CancellationToken`; there are **no synchronous overloads**.

The surface is intentionally minimal — a handful of interfaces plus one
dispatcher — so the maintenance cost is low and fully under our control.

## Consequences

- **Easier:** No paid or restrictively licensed dependency; one fewer third-party
  package to track and vet. The layer stays framework-agnostic and reusable. We
  control return conventions, cancellation, exception policy, and behavior
  ordering.
- **Harder:** We own the dispatcher code and its tests, including reflection/DI
  wiring that a library would otherwise provide. Contributors must not
  reintroduce a third-party mediator.

## Alternatives considered

- **MediatR** — rejected: current versions are commercially licensed, the exact
  situation ADR-0014 established we avoid.
- **Other mediator libraries** (e.g. MassTransit Mediator, Wolverine, source-generator
  mediators) — each adds a third-party dependency, its own opinions, and future
  license/maintenance risk; using MassTransit's in-process mediator would also
  couple a framework-agnostic layer to MassTransit (currently our *messaging*
  choice, ADR-0004). Rejected in favor of a minimal, dependency-free implementation.
- **No mediator (inject handlers directly)** — viable, but loses a single,
  uniform place to apply cross-cutting pipeline behaviors (e.g. exception-to-Result
  translation). Rejected in favor of a thin dispatcher.
