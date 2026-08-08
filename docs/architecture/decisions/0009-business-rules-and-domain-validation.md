# 0009. Business rules and domain validation

- **Status:** Accepted
- **Date:** 2026-06-24
- **Amended:** 2026-08-06 (a `null` rule throws — see the note below)
- **Amended:** 2026-08-08 (the `params` overloads collect every violation; every rule carries a `Code`, validation rules also a `Target`)

## Context

Domains need to express two different kinds of constraint:

1. **Business rules / invariants** — conditions that must always hold for the domain to be in a valid state (e.g. "a workout session cannot be completed before it is started").
2. **Domain validation** — constraints on incoming values (e.g. "a recipe name must not be empty").

Conflating the two makes it impossible for callers to react differently (for example, mapping them to different responses), and obscures intent in the domain code.

## Decision

Model the two concepts separately, each with its own rule interface and exception, evaluated by a single `RuleChecker`:

| Concept | Rule interface | Predicate | Exception |
|---|---|---|---|
| Business rule / invariant | `IBusinessRule` | `IsBroken()` | `BusinessRuleViolationException` |
| Domain validation | `IDomainValidationRule` | `IsInvalid()` | `DomainValidationException` |

```csharp
RuleChecker.Check(new RecipeNameMustNotBeEmpty(name));
RuleChecker.Check(rule1, rule2, rule3);
```

Rules are small, named, self-describing types carrying their own `Message`.

## Consequences

- Intent is explicit at the call site: a reader sees *which kind* of constraint is being enforced.
- Callers (and tests) can distinguish invariant violations from validation failures via the exception type.
- Rules are reusable, individually testable units rather than inline `if` statements.
- The `params` overload evaluates every rule in order and reports all violations at once, so message ordering is deterministic and a caller sees every problem in one round trip.

## Alternatives considered

- **A single rule interface / single exception:** simpler, but loses the business-vs-validation distinction that callers need.
- **Inline `if` + `throw`:** scatters invariants through the code, harder to reuse and test, and less self-documenting.
- **Returning a result object instead of throwing:** reasonable in the application layer, but within the domain a thrown exception keeps invariant enforcement unambiguous and prevents an aggregate from continuing in an invalid state.

## Amendment, 2026-08-06: a `null` rule is a bug, not a satisfied rule

`RuleChecker` evaluated `rule?.IsBroken() == true`, so a `null` rule passed silently. That is the
one failure mode the validation layer exists to prevent: a factory that accidentally returns
`null` — a mistyped conditional, a not-yet-assigned field, a collection initialiser with a hole —
produced a *valid* domain object instead of an exception, and the resulting bad state surfaced
much later, far from its cause.

All four overloads now guard with `ArgumentNullException.ThrowIfNull`, and the `params`
overloads guard the array itself as well. Nothing else changes: the first broken rule still
throws and the remaining rules are still not evaluated. No test had pinned the old tolerance,
which is itself evidence that it was never a decision.

## Amendment, 2026-08-08: the `params` overloads collect, and a validation rule names its field

A user filling in a form gets one error at a time when the domain stops at the first broken
rule, so the round trips multiply with the number of mistakes. Worse, the caller could not
tell *which field* an error belonged to: a `Failure` carried a message and a code shared by
every validation error in the system, so a UI had no way to place the message next to the
input that caused it. Multi-error reporting was unreachable from the domain, which is where
the rules live.

Three changes, and no more than three:

1. **Both `params` overloads evaluate every rule and collect the broken ones.** They throw
   once, at the end, when at least one rule failed. Order is call order, so the reported
   sequence is still deterministic. The single-rule overloads are unchanged, and an empty
   array still throws nothing. Rules are therefore **independent by contract**: a rule must
   tolerate being evaluated even when an earlier rule in the same call already failed. Where
   one rule only makes sense after another has passed, the author writes **two consecutive
   `Check` calls** — a pre-condition pass and a dependent pass — which is exactly what
   `Gadget` and `GadgetComponent` already do for a validation rule followed by a business
   rule. That staging is domain design, not framework behaviour.
2. **The `null` guard moves ahead of the evaluation.** The whole array is checked before the
   first rule runs. Otherwise whether a `null` surfaces as an `ArgumentNullException` or
   disappears behind collected domain errors would depend on its position in the argument
   list.
3. **Both rule interfaces gain `string Code`; only `IDomainValidationRule` gains `string? Target`.**
   `Code` is the stable, machine-readable identifier of the rule, and it is the rule's own — the
   whole point of naming a constraint is that a client can react to *that* constraint. `Target`
   names the field the rule is about and is `null` for a rule spanning several fields;
   `IBusinessRule` does **not** get it, because an invariant is a statement about the aggregate,
   not about a field, and forcing a field name onto it would invite exactly the confusion this
   ADR exists to prevent. That argument covers `Target` and **only** `Target`: a code identifies
   the rule, not a field, so extending the field argument to it would have left every business
   rule violation in the system sharing one technical code — the very defect this amendment set
   out to fix, half-fixed.

Both exceptions now carry `IReadOnlyList<RuleViolation> Violations`, where
`RuleViolation(string Code, string? Target, string Message)` is the shared carrier. `Code` is
**not** nullable: every violation that originates from a rule has the rule's code, and the two
exceptions' message-only constructors — which CA1032 requires and which carry no rule — fill in
a `FallbackCode` constant declared on the exception itself (`domain.validation` /
`domain.business_rule`). The translating layer therefore no longer substitutes anything and has
one code path instead of two; it reuses those same constants rather than declaring its own.
For a business rule violation `Target` is always `null`. `Exception.Message` is the single violation's message verbatim
when there is exactly one, and the messages joined by `"; "` otherwise — structured access is
via `Violations`, not by parsing the message.

The two rule kinds are **not merged** and are **not mixed in one call**: each overload takes
one kind and throws its own exception. The consequence is worth stating explicitly, because
the transport layer depends on it: **one handler invocation raises at most one exception, so
every `Failure` in a failed `Result` shares one category**, and the status code stays
unambiguous.
