# 0009. Business rules and domain validation

- **Status:** Accepted
- **Date:** 2026-06-24

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
RuleChecker.Check(new RecipeNameMustNotBeEmpty(name)); // business rule
RuleChecker.Check(rule1, rule2, rule3);                // params overload; short-circuits on first failure
```

Rules are small, named, self-describing types carrying their own `Message`.

## Consequences

- Intent is explicit at the call site: a reader sees *which kind* of constraint is being enforced.
- Callers (and tests) can distinguish invariant violations from validation failures via the exception type.
- Rules are reusable, individually testable units rather than inline `if` statements.
- The `params` overload evaluates rules in order and throws on the first failure, so message ordering is deterministic.

## Alternatives considered

- **A single rule interface / single exception:** simpler, but loses the business-vs-validation distinction that callers need.
- **Inline `if` + `throw`:** scatters invariants through the code, harder to reuse and test, and less self-documenting.
- **Returning a result object instead of throwing:** reasonable in the application layer, but within the domain a thrown exception keeps invariant enforcement unambiguous and prevents an aggregate from continuing in an invalid state.