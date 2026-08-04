# 0018. Three building block packages: Domain, Application, Infrastructure

- **Status:** Accepted
- **Date:** 2026-07-25

## Context

The Building Blocks are the reusable, VitalSync-agnostic platform that every
microservice builds upon. Earlier drafts of the documentation described up to five
separate packages — `Domain`, `Application`, `EventProcessing`, `Persistence`, and
`Infrastructure` — each with its own project and dependency edges.

That split turned out to be artificial. The real, meaningful boundary between the
Building Blocks is **not** functional (persistence vs. event processing vs. …); it
is about **purity and third-party dependencies**:

- Some code must stay **pure** — no framework, no third party — so it remains a
  clean, reusable core (`Domain`) and a framework-agnostic use-case contract layer
  (`Application`).
- Everything else is **concrete, framework-bound infrastructure** — unit of work,
  generic repositories, event/message dispatching, broker wrappers — and it all
  shares the same third-party dependencies and is always deployed together.

Splitting the framework-bound code into `EventProcessing` and `Persistence` added
project count and dependency edges without buying any isolation: those packages
would always be referenced together, depend on the same third parties, and blur the
one rule we actually care about — *contracts stay pure; implementations live in one
outer layer.*

## Decision

Consolidate the Building Blocks into exactly **three** packages, drawn along the
purity / third-party-dependency boundary:

- **`BuildingBlocks.Domain`** — the pure, BCL-only core. Tactical DDD primitives:
  entities, aggregate roots (state-stored and event-sourced), domain events, value
  objects, strongly typed identifiers, domain exceptions, business-rule/validation
  abstractions, and the `IClock` abstraction. It has **no** dependencies and
  references **no** third party.

- **`BuildingBlocks.Application`** — the framework-agnostic use-case layer. CQRS
  abstractions (commands, queries, handlers), the pipeline-behavior contract, the
  dispatcher (`ISender`) contract, and the `Result` / `Failure` model. It depends
  **only** on `Domain` and still references **no** third party (contracts only).

- **`BuildingBlocks.Infrastructure`** — the single outer layer that holds **all**
  reusable, framework-bound, third-party-backed implementations that are still
  **independent of any VitalSync domain logic**. This includes, among others:
  - the **unit of work**;
  - **generic repositories** for EF Core and for the (still TBD) event-store tool;
  - **domain event dispatching**;
  - **integration event dispatching**;
  - the **RabbitMQ** wrapper / messaging transport;
  - the DI-based CQRS **dispatcher** and **pipeline behaviors** that implement the
    `Application` contracts.

  It depends on `Domain` and `Application`, and it is where every third-party
  dependency of the platform lives.

There are **no** `BuildingBlocks.EventProcessing` or `BuildingBlocks.Persistence`
packages. Persistence, event processing, and messaging are **capabilities inside
`Infrastructure`**, not standalone building blocks.

The guiding rule is simple: **if it is reusable and VitalSync-agnostic but needs a
framework or third party, it belongs in `Infrastructure`; if it is tied to a
specific service's domain logic, it belongs in that service, not in the Building
Blocks at all.**

## Consequences

- **Easier:** Fewer projects, fewer dependency edges, and one obvious home for
  every concrete, third-party-backed concern. The boundary — "pure contracts in
  `Domain`/`Application`; all framework-bound implementations in `Infrastructure`"
  — is trivial to state and to enforce. All third-party dependencies are localized
  to a single package.
- **Harder:** `Infrastructure` is broad, so internal organization (folders /
  namespaces for unit of work, repositories, dispatching, and messaging) matters
  more to keep it navigable. Shipping one infrastructure capability independently
  in the future would require re-splitting — accepted as unlikely, since these
  always travel and deploy together.

## Alternatives considered

- **Five packages** (`Domain`, `Application`, `EventProcessing`, `Persistence`,
  `Infrastructure`) — rejected: the split was functional rather than along the real
  purity boundary, adding project and dependency-edge overhead without isolation,
  since the framework-bound pieces share third parties and always ship together.
- **Four packages** (fold only `EventProcessing` into `Persistence`, or vice
  versa) — rejected: still leaves an arbitrary boundary between framework-bound
  layers that always travel together.
- **Two packages** (fold `Application` into `Infrastructure`) — rejected: this
  sacrifices the valuable purity boundary that keeps the use-case contracts
  framework-agnostic and reusable.

> **The independence promise now holds literally (amendment 2026-08-05).** Until now
> `BuildingBlocks.Infrastructure` contained one VitalSync string: the integration-event
> exchange name `vitalsync.integration-events`, defended in
> [ADR-0023](./0023-wolverine-messaging-transport.md) as safer than letting each host
> restate it. The exchange name is now a host argument, VitalSync defines it once in
> `VitalSync.ServiceDefaults`, and `vitalsync` no longer appears anywhere under
> `BuildingBlocks/src`. The rule for future contributions: a product name in Building
> Blocks is a defect, not a shortcut � if a value is deployment-specific, the host names
> it.
