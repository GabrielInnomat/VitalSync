# 0008. Entity identity and equality

- **Status:** Accepted
- **Date:** 2026-06-24
- **Amended:** 2026-06-25 (aggregate identity now sourced from the state — see [ADR-0010](./0010-aggregate-state-object.md))
- **Amended:** 2026-07-22 (keys split into `IEntityKey` marker + `IEntityKey<TValue>`; identity validation is type-agnostic via `IsEmpty`; aggregate roots split into two bases — see the note below and [ADR-0012](./0012-optional-event-sourcing-aggregate.md))

## Context

Entities and aggregates need stable identity and well-defined equality. Using primitive keys (`Guid`) invites accidental misuse (passing one aggregate's id where another's is expected). Mutable identity would break equality and change tracking.

A complication arises from the decision to hold an aggregate's state in a dedicated state object that *owns the identity* (see [ADR-0010](./0010-aggregate-state-object.md)): for aggregates, the identity is not available at construction time.

## Decision

- **Strongly typed keys.** Every key implements `IEntityKey` and is a `readonly record struct`. Each aggregate has its own key type, so mixing keys of different aggregates is a compile-time error.

- **Identity location differs by kind:**
  - **Non-aggregate entities** (`Entity<TKey>`) assign `Id` once in the **constructor**, get-only, with an empty-identity guard in that constructor.
  - **Aggregate roots** derive or receive `Id` depending on the base (see amendment below).

- **Identity validation happens where the id becomes available.** For constructor-set identity the guard runs in the constructor; for state-owned identity the check runs after each event is applied.

- **Identity equality.** Entities and aggregate roots are equal iff they are the same concrete type and have equal ids. `Equals(object?)` and `GetHashCode()` are `sealed`.

> **Implementation note (amendment 2026-07-22):** The original wording referred to a single `Guid`-backed `IEntityKey` and a single aggregate base `AggregateRoot<TKey, TState>`. Both have since evolved (see [ADR-0012](./0012-optional-event-sourcing-aggregate.md)):
>
> - **Keys are no longer `Guid`-locked.** `IEntityKey` is now a **non-generic marker** that also declares `bool IsEmpty { get; }`; `IEntityKey<TValue>` (where `TValue : notnull`) exposes the underlying `Value`. The value type may be `Guid`, `int`, `string`, or any `notnull` type. Each key defines its own `IsEmpty` rule (e.g. `Value == Guid.Empty`, `Value <= 0`, `string.IsNullOrWhiteSpace(Value)`).
> - **Identity validation is type-agnostic.** The guards no longer inspect the raw value type; they call `id.IsEmpty` (or `State.Id.IsEmpty`), so the base classes stay value-type-agnostic. The previous "empty-GUID" wording should be read as "empty per the key's `IsEmpty` rule".
> - **Aggregate roots split into two bases.**
>   - **`AggregateRoot<TKey>`** (state-modeled) assigns `Id` in the **constructor**, with the `IsEmpty` guard there — like `Entity<TKey>`.
>   - **`EventSourcedAggregateRoot<TKey, TState>`** (event-modeled) derives `Id` from its **state** (`Id => State.Id`); a new aggregate starts with a default `Id`, the first applied event sets it, and the `IsEmpty` check runs at every transition.
>
> The equality rule (same concrete type + equal ids, `sealed` `Equals`/`GetHashCode`) is unchanged and applies to `Entity<TKey>`, `AggregateRoot<TKey>`, and `EventSourcedAggregateRoot<TKey, TState>`.

## Consequences

- Whole classes of identifier-mix-up bugs become compile-time errors.
- Identity cannot change after it is established, keeping equality and hashing stable.
- Equality is type-sensitive: instances of different types with the same id are never equal.
- For event-sourced aggregates, two *un-created* instances (both `Id == default`) compare equal until their creation events run; in practice aggregates are created only via factories that immediately raise the creation event.
- Keys, being `record struct`, get value equality for free; the `struct` constraint on `TKey` keeps keys as value types throughout.
- Persistence must map strongly typed keys to their underlying primitive (handled by the Persistence building block).

## Alternatives considered

- **Primitive keys (`Guid`):** simplest but unsafe; misuse compiles and may fail only at runtime.
- **A shared base key type for all aggregates:** still allows mixing different aggregates' ids; loses type safety. (Also impossible as an `abstract record struct`, since value types cannot be inherited.)
- **Mutable `Id` (`protected set`):** rejected — identity mutation breaks equality and tracking guarantees.
- **Identity in the entity constructor for event-sourced aggregates too:** incompatible with state-owned identity (ADR-0010); the constructor has no id to assign for a brand-new aggregate.
- **A post-creation `EnsureCreated()` validity check:** rejected in favor of validating at each transition, so validity is intrinsic to every state change rather than a separate step.
