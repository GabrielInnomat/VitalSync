# 0017. Application error handling: domain exceptions translated to Result; unexpected errors handled globally

- **Status:** Accepted
- **Date:** 2026-07-24

## Context

The Domain enforces constraints by **throwing** exceptions —
`BusinessRuleViolationException` and `DomainValidationException` — via `RuleChecker`
(see [ADR-0009](./0009-business-rules-and-domain-validation.md)). The Application
layer, however, exposes command/query outcomes as a uniform `Result` / `Result<T>`
(see [ADR-0016](./0016-remove-common-result-in-application.md)).

If domain exceptions were allowed to escape _around_ `Result`, callers (the BFF,
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
   directly for expected outcomes such as _not found_ or _conflict_.

2. **Unexpected errors → thin global handler.**
   Any other exception (bugs, infrastructure/transport failures) is **not**
   wrapped in a `Result`. It bubbles to a thin **global exception handler** in the
   service host, which returns a generic internal error. There is deliberately **no**
   `Unexpected` Failurecategory — unexpected failures never become a `Result`.

### Result Failure shape

A failed `Result` carries **one or more** `Failure` values. Each `Failure` has:

- `Code` — a stable, machine-readable string (e.g. `recipe.name_required`) for
  i18n and specific client handling;
- `Message` — a human-readable description;
- `Category` — an `FailureCategory` enum, one of:
  `Validation`, `BusinessRule`, `NotFound`, `Conflict`.

The translation behavior maps `DomainValidationException` → `Validation` and
`BusinessRuleViolationException` → `BusinessRule`; handlers return `NotFound` /
`Conflict` for those expected outcomes.

### Transport status mapping is not an Application concern

`BuildingBlocks.Application` never references HTTP or gRPC. Mapping
`FailureCategory` to a transport status code is owned by the boundary:

- the **BFF** maps `FailureCategory` → **HTTP status code** (the only place HTTP
  status codes are defined);
- the **service host** maps `FailureCategory` → **gRPC status**.

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

> **Amendment (2026-08-05) — the failure factory is named `Failed`.**
> The factory that builds a failed result was called `Failure`, which collided with
> everything around it: the type `Failure`, the property `Failures`, and the property
> `IsFailure` all live in the same namespace, so `static Result Failure(Failure failure)`
> read as a tautology and `Result<T>` had to hide it with `static new`. It is now
> `Result.Failed(...)` / `Result<T>.Failed(...)`; `Failure` remains the name of the error
> **value**, `Failed` is what you **do** with it. Nothing else about this ADR changes —
> the two channels, the four categories and the pipeline translation are untouched.
> The rename is purely mechanical and the compiler finds every call site.

> **Amendment (2026-08-08) — a fifth category, `Forbidden`; `Unexpected` stays rejected.**
> A denied authorization is an expected outcome that a handler decides, so it belongs on the
> `Result` channel like _not found_ and _conflict_ — it had no category and reached the
> transport as the adapters' fallback status. `FailureCategory` therefore gains `Forbidden`
> (with `Failure.Forbidden(...)`, mapped to gRPC `PermissionDenied`), and the list in this ADR
> reads `Validation`, `BusinessRule`, `NotFound`, `Conflict`, `Forbidden`. **Nothing else
> changes**, and in particular the rejection of `Unexpected` above stands: an unexpected error
> remains an exception for the global handler, because degrading it to a `Result` is exactly
> the second failure channel this ADR closes. `Unauthorized` (401) likewise stays out —
> authentication is the host's business and never reaches this layer.
>
> Worth recording, because it was assumed otherwise: extending this enum is **not**
> compiler-checked at the boundary and cannot be. A `switch` expression over an enum always
> requires a discard arm (CS8509, since an enum can carry any `int`), and that arm silently
> absorbs every value added later. The guarantee is therefore a pair of run-time guards —
> each adapter's mapping is walked over `Enum.GetValues<FailureCategory>()` and must not fall
> through, and every declared category must have a factory of its own name on `Failure`. A
> future category is added by making those two tests pass.

> **Amendment (2026-08-08) — a failed result regularly carries several failures, and a failure may name a field.**
> ADR-0009's amendment of the same date makes `RuleChecker`'s `params` overloads collect every
> broken rule instead of stopping at the first, so a single domain exception now arrives with a
> list of violations. `ExceptionToResultBehavior` maps **one `Failure` per violation** rather
> than one per exception, and `RequestPipeline<TResponse>.Failed` gains an overload taking
> `IReadOnlyList<Failure>` (the single-failure overload stays, and the dispatcher's factory is
> now `Result.Failed(IReadOnlyList<Failure>)`). `Failure` gains an optional `Target`:
> the name of the field the error is about, `null` when the error spans several fields or is an
> invariant. The `Code` is always the **rule's own** — for a business rule too, since a code
> identifies the constraint rather than a field. The behavior's `ValidationFailureCode` /
> `BusinessRuleFailureCode` constants survive only as aliases of the exceptions' `FallbackCode`,
> which applies to the message-only constructors CA1032 forces on both exceptions and which
> carry no rule.
>
> The category question this raises has a clean answer, and it is a consequence of ADR-0009
> keeping the two rule kinds separate: a handler invocation raises **at most one** exception, and
> each exception belongs to exactly one category, so **every `Failure` in a failed `Result`
> shares one category**. There is no precedence rule to invent at the boundary — the adapter reads
> the category off the first failure and the status code is unambiguous. The remaining failures
> are not lost: each adapter writes them all into the gRPC **trailers** (`failure-count` plus
> `failure-{i}-code` / `-message` / `-target`, the target trailer omitted when `null`), so
> a client can place every message next to the input that caused it. Trailers were chosen over the
> gRPC Rich Error Model because the latter needs an extra package and a protobuf `Any`, and this
> repository's samples are code-first.