# 0017. Application error handling: domain exceptions translated to Result; unexpected errors handled globally

- **Status:** Accepted
- **Date:** 2026-07-24

## Context

The Domain enforces constraints by **throwing** exceptions —
`BusinessRuleViolationException` and `DomainValidationException` — via `RuleChecker`
(see [ADR-0009](./0009-business-rules-and-domain-validation.md)). The Application
layer, however, exposes command/query outcomes as a uniform `Result` / `Result<T>`
(see [ADR-0016](./0016-remove-common-result-in-application.md)).

If domain exceptions were allowed to escape *around* `Result`, callers (the BFF,
the frontend) would face **two** different failure channels — exceptions and
failed results — for what are, from the caller's perspective, the same class of
expected domain errors. We need a single, predictable failure channel for expected
errors, while still letting genuinely unexpected errors (bugs, infrastructure
failures) surface as failures rather than being disguised as domain outcomes.

## Decision

Adopt a two-tier error-handling model:

1. **Expected domain errors → `Result.Failure` (via a pipeline behavior).**
   The Domain continues to throw (ADR-0009 is unchanged). An
   `ExceptionToResultBehavior` in the Application pipeline catches
   `BusinessRuleViolationException` and `DomainValidationException` and converts
   them into `Result.Failure(...)`. Handlers may also return `Result.Failure`
   directly for expected outcomes such as *not found* or *conflict*.

2. **Unexpected errors → thin global handler.**
   Any other exception (bugs, infrastructure/transport failures) is **not**
   wrapped in a `Result`. It bubbles to a thin **global exception handler** in the
   service host, which returns a generic internal error. There is deliberately **no**
   `Unexpected` error category — unexpected failures never become a `Result`.

### Result error shape

A failed `Result` carries **one or more** `Error` values. Each `Error` has:

- `Code` — a stable, machine-readable string (e.g. `recipe.name_required`) for
  i18n and specific client handling;
- `Message` — a human-readable description;
- `Category` — an `ErrorCategory` enum, one of:
  `Validation`, `BusinessRule`, `NotFound`, `Conflict`.

The translation behavior maps `DomainValidationException` → `Validation` and
`BusinessRuleViolationException` → `BusinessRule`; handlers return `NotFound` /
`Conflict` for those expected outcomes.

### Transport status mapping is not an Application concern

`BuildingBlocks.Application` never references HTTP or gRPC. Mapping
`ErrorCategory` to a transport status code is owned by the boundary:

- the **BFF** maps `ErrorCategory` → **HTTP status code** (the only place HTTP
  status codes are defined);
- the **service host** maps `ErrorCategory` → **gRPC status**.

This keeps the Application layer framework-agnostic and reusable, and lets REST
and gRPC map the same semantic categories independently.

## Consequences

- **Easier:** One uniform failure channel (`Result`) for all expected domain
  errors; the frontend/BFF handle failures consistently and map categories to
  status codes in one place. The Domain stays exception-based and unchanged.
- **Harder:** The pipeline must register the `ExceptionToResultBehavior` (typically
  first). Contributors must be disciplined: expected domain errors flow through
  `Result`; only truly unexpected errors are allowed to throw to the global handler.

## Alternatives considered

- **Let domain exceptions bubble to a global handler for everything** — rejected:
  splits expected failures across two channels (exceptions and `Result`),
  complicating callers and undermining the `Result` convention.
- **Return `Result` from the Domain itself** — rejected: within the Domain a thrown
  exception keeps invariant enforcement unambiguous (ADR-0009); translation to
  `Result` belongs at the Application boundary.
- **Encode HTTP/gRPC status in the Application error** — rejected: couples a
  framework-agnostic, reusable layer to a transport; mapping belongs to the
  BFF/service host.
- **Include an `Unexpected` category** — rejected: it would invite wrapping bugs in
  `Result.Failure`; unexpected errors must remain exceptions handled globally.
