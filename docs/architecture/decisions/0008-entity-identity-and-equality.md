# 0008. Entity identity and equality

- **Status:** Accepted
- **Date:** 2026-06-24
- **Amended:** 2026-06-25 (aggregate identity now sourced from the state — see [ADR-0010](./0010-aggregate-state-object.md))

## Context

Entities and aggregates need stable identity and well-defined equality. Using primitive keys (`Guid`) invites accidental misuse (passing one aggregate's id where another's is expected). Mutable identity and reference equality lead to subtle bugs in collections, deduplication, and persistence tracking.

A complication arises from the decision to hold an aggregate's state in a dedicated state object that *owns the identity* (see [ADR-0010](./0010-aggregate-state-object.md)): for aggregates, the identity cannot be assigned in the constructor, because a newly created aggregate starts from an empty state and only obtains its identity when its first event is applied.

## Decision

- **Strongly typed keys.** Every key implements `IEntityKey` (a `Guid`-backed contract) and is a `readonly record struct`. Each aggregate has its own key type, so mixing keys of different aggregates is a **compile-time error**.

- **Identity location differs by kind:**
  - **Non-aggregate entities** (`Entity<TKey>`) assign `Id` once in the **constructor**, get-only, with an empty-GUID guard in that constructor.
  - **Aggregate roots** (`AggregateRoot<TKey, TState>`) derive `Id` from their **state** (`Id => State.Id`). A new aggregate starts with a default `Id`; the first applied event sets it.

- **Identity validation for aggregates happens at every transition.** Because the constructor cannot guard the id, the empty-GUID check runs immediately after each event is applied (in both `RaiseEvent` and `LoadFromHistory`). The first event must set a non-empty `Id`; no later event may blank it. There is **no** separate post-creation check.

- **Identity equality.** Both `Entity<TKey>` and `AggregateRoot<TKey, TState>` are equal iff they are the same concrete type and have equal ids. `Equals(object?)` and `GetHashCode()` are `sealed` to prevent inconsistent overrides in subclasses.

## Consequences

- Whole classes of identifier-mix-up bugs become compile-time errors.
- Identity cannot change after it is established, keeping equality and hashing stable.
- Equality is type-sensitive: instances of different types with the same id are never equal.
- For aggregates, two *un-created* instances (both `Id == default`) compare equal until their creation events run; in practice aggregates are created only via factories that immediately raise the creation event, so this is a degenerate case.
- Keys, being `record struct`, get value equality for free; the `struct` constraint on `TKey` keeps keys as value types throughout.
- Persistence must map strongly typed keys to their underlying primitive (handled by the Persistence building block).

## Alternatives considered

- **Primitive keys (`Guid`):** simplest but unsafe; misuse compiles and may fail only at runtime.
- **A shared base key type for all aggregates:** still allows mixing different aggregates' ids; loses type safety. (Also impossible as an `abstract record struct`, since value types cannot be inherited.)
- **Mutable `Id` (`protected set`):** rejected — identity mutation breaks equality and tracking guarantees.
- **Identity in the entity constructor for aggregates too:** incompatible with state-owned identity (ADR-0010); the constructor has no id to assign for a brand-new aggregate.
- **A post-creation `EnsureCreated()` validity check:** rejected in favor of validating at each transition, so validity is intrinsic to every state change rather than a separate step.