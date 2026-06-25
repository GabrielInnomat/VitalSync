# BuildingBlocks.Domain — Technical Reference

`BuildingBlocks.Domain` is the foundational building block of the platform. It provides the tactical Domain-Driven Design primitives that every microservice's domain layer builds upon, and it is deliberately **free of any infrastructure or third-party dependency** (the project declares no package references).

A central design goal of this block is that **a single aggregate model serves both persistence worlds** — event sourcing (ES) and state-stored (EF Core) — so that the choice of persistence strategy does not leak into the domain. See [ADR-0011](./decisions/0011-unified-aggregate-for-es-and-ef.md).

> Scope: this document describes the _domain_ building block only. Application, Persistence, Infrastructure, and EventProcessing are documented separately.

## Design goals

- Provide consistent, reusable domain primitives across all services.
- Keep the domain layer **pure** — no framework, no infrastructure, BCL only.
- Make key domain rules **structural** (enforced by the type system) rather than conventional.
- Provide **one aggregate base** usable by both event-sourced and state-stored services.
- Remain **independent of VitalSync** so the block is reusable in future projects.

## Contents

| Type                               | Kind            | Responsibility                                                                       |
| ---------------------------------- | --------------- | ------------------------------------------------------------------------------------ |
| `IEntityKey`                       | interface       | Contract for a strongly typed key backed by a `Guid`.                                |
| `IEntity<TKey>`                    | interface       | An entity with a strongly typed identity.                                            |
| `Entity<TKey>`                     | abstract class  | Base for non-aggregate entities: constructor-set identity, guard, identity equality. |
| `IState<TKey>`                     | interface       | An aggregate's state: owns the identity and the event-apply ("evolve") logic.        |
| `IAggregateRoot<TKey>`             | interface       | Marker for an aggregate root; exposes events **read-only**.                          |
| `IEventSourcedAggregateRoot<TKey>` | interface       | Optional marker exposing `Version` + `LoadFromHistory` for ES infrastructure.        |
| `AggregateRoot<TKey, TState>`      | abstract class  | The single aggregate base for both ES and EF Core.                                   |
| `IHasDomainEvents`                 | interface       | Read-only access to an aggregate's domain events.                                    |
| `IDomainEventsManager`             | interface       | Privileged contract that can **clear** events (infrastructure-only).                 |
| `IDomainEvent`                     | interface       | Pure business event contract (`EventId`, `OccurredAt`).                              |
| `DomainEvent`                      | abstract record | Convenience base supplying `EventId` and clock-based `OccurredAt`.                   |
| `IClock`                           | interface       | Abstraction over "now" for deterministic time.                                       |
| `IBusinessRule`                    | interface       | An invariant that can be _broken_.                                                   |
| `IDomainValidationRule`            | interface       | A validation constraint that can be _invalid_.                                       |
| `RuleChecker`                      | static class    | Evaluates rules and throws the matching exception.                                   |
| `BusinessRuleViolationException`   | exception       | Raised when a business rule is broken.                                               |
| `DomainValidationException`        | exception       | Raised when a domain validation rule is invalid.                                     |

## Identity and keys

### Strongly typed keys

Every aggregate/entity key implements `IEntityKey` and is a `readonly record struct`:

```csharp
public readonly record struct RecipeId(Guid Value) : IEntityKey;
```

Because each aggregate has its own key type, passing the wrong key is a **compile-time error**: a `RecipeId` is not an `IngredientId`, even though both wrap a `Guid`. See [ADR-0008](./decisions/0008-entity-identity-and-equality.md).

### Where identity lives

There are two cases, by design:

- **Non-aggregate entities** (`Entity<TKey>`) receive their identity in the **constructor** and expose it get-only. The empty-GUID guard runs in that constructor.
- **Aggregate roots** (`AggregateRoot<TKey, TState>`) take their identity from their **state** (`Id => State.Id`). A freshly created aggregate therefore starts with a default `Id`; the **first applied event sets it**. See [ADR-0008](./decisions/0008-entity-identity-and-equality.md) and [ADR-0010](./decisions/0010-aggregate-state-object.md).

### Identity validation for aggregates

Because an aggregate's identity comes from its state, the empty-GUID guard cannot run at construction. Instead it runs **at every state transition**: immediately after an event is applied (in both `RaiseEvent` and `LoadFromHistory`), the resulting `State.Id` must be non-empty, otherwise a `DomainValidationException` is thrown.

This means:

- The **first (creation) event must set a non-empty `Id`**, or the first `RaiseEvent` throws.
- **No later event may blank the `Id`** — every transition is validated.
- **Replaying a corrupt stream** that yields an empty `Id` fails immediately during rehydration.

There is deliberately **no** separate post-creation validity check; validity is intrinsic to each transition.

### Identity equality

Both `Entity<TKey>` and `AggregateRoot<TKey, TState>` implement identity equality: two instances are equal when they are the **same concrete type** and have **equal ids**. `Equals(object?)` and `GetHashCode()` are `sealed` so derived types cannot reintroduce inconsistent equality.

```text
left.Equals(right)  ⇔  left.GetType() == right.GetType()  ∧  left.Id == right.Id
```

> Note: two _un-created_ aggregates both have `Id == default` and would compare equal until their creation events run. In practice aggregates are only created through factories that immediately raise the creation event (after which `RaiseEvent` has enforced a non-empty `Id`), so this is a degenerate case.

## The state object

Every aggregate's state implements `IState<TKey>`:

```csharp
public interface IState<out TKey>
    where TKey : struct, IEntityKey
{
    TKey Id { get; }                              // the aggregate's identity
    IState<TKey> Apply(IDomainEvent domainEvent); // the evolve/apply function
}
```

The state object exists to keep large aggregates maintainable. All **apply/evolution logic lives on the state**, so the aggregate class contains only the public command API (the behavior invoked by commands), not the per-event state-mutation code. For a large aggregate this avoids the otherwise unavoidable "two methods per event" growth (one command method + one apply branch) inside the aggregate. See [ADR-0010](./decisions/0010-aggregate-state-object.md).

State implementations are expected to be **immutable**: `Apply` returns the next state (`this with { … }`) rather than mutating in place.

```csharp
public sealed record RecipeState : IState<RecipeId>
{
    public RecipeId Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public static RecipeState Empty => new();

    public IState<RecipeId> Apply(IDomainEvent e) => e switch
    {
        RecipeCreated created => this with { Id = created.RecipeId, Name = created.Name },
        RecipeRenamed renamed => this with { Name = renamed.NewName },
        _ => this
    };
}
```

## Aggregates and domain events

### The single aggregate base

`AggregateRoot<TKey, TState>` is the **one** base class for both persistence worlds:

```csharp
public abstract class AggregateRoot<TKey, TState>
    : IAggregateRoot<TKey>, IDomainEventsManager, IEquatable<AggregateRoot<TKey, TState>>
    where TKey : struct, IEntityKey
    where TState : IState<TKey>
```

- It holds the current `State`, the `Version` (stream position), and the private uncommitted-events list.
- `Id` is derived from `State`.
- State changes **only** via:
  - `RaiseEvent(e)` — apply the event to the state, validate identity, advance the version, and record the event (new behavior from a command);
  - `LoadFromHistory(history)` — replay a persisted stream to rebuild state (rehydration; records nothing).
- It exposes events **read-only** and implements clearing **explicitly** (see below).

How each world uses it:

|                   | Event-sourced                       | State-stored (EF Core)           |
| ----------------- | ----------------------------------- | -------------------------------- |
| Persists          | the raised events                   | the `TState` object              |
| Rebuilds state by | `LoadFromHistory` (replay)          | loading the stored `TState`      |
| Uses `Version`    | yes (stream position / concurrency) | unused                           |
| Uses `Apply`      | yes (replay + new events)           | inert (state is loaded directly) |

The aggregate author does **not** choose a base class per strategy; they always use `AggregateRoot<TKey, TState>`. See [ADR-0011](./decisions/0011-unified-aggregate-for-es-and-ef.md).

### Ownership rule

> The aggregate is the **sole owner** of its domain events. Only the aggregate may raise events; only a privileged infrastructure contract may clear them; everyone else gets a **read-only** view.

This is realized with two interfaces and explicit implementation:

```text
IHasDomainEvents          → IReadOnlyCollection<IDomainEvent> DomainEvents   (everyone)
        ▲
        │
IDomainEventsManager      → void ClearDomainEvents()                          (infrastructure only)
```

- `IAggregateRoot<TKey>` inherits **only** `IHasDomainEvents`. Application code holding an aggregate therefore sees `DomainEvents` and **cannot** see `ClearDomainEvents()`.
- `AggregateRoot<TKey, TState>` _also_ implements `IDomainEventsManager`, but **explicitly**:

```csharp
void IDomainEventsManager.ClearDomainEvents() => _domainEvents.Clear();
```

- Raising is `protected` (`RaiseEvent`), so only the aggregate itself can add events.

### Access matrix

| Caller holds…                               | Read events? | Clear events?              | Raise events?               |
| ------------------------------------------- | ------------ | -------------------------- | --------------------------- |
| The concrete aggregate (e.g. `Recipe`)      | ✅           | ❌ (not on surface)        | ❌ (only internally)        |
| `IAggregateRoot<TKey>`                      | ✅           | ❌                         | ❌                          |
| `IHasDomainEvents`                          | ✅           | ❌                         | ❌                          |
| `IDomainEventsManager` (cast)               | ✅           | ✅                         | ❌                          |
| A subclass of `AggregateRoot<TKey, TState>` | ✅           | ❌ (explicit, not visible) | ✅ (`protected RaiseEvent`) |

### Why clearing is separated

The persistence layer collects events, dispatches them, and clears them **only after a successful save**. Keeping `ClearDomainEvents()` off the public surface prevents any application layer from clearing prematurely (which would silently drop undispatched events). See [ADR-0006](./decisions/0006-aggregate-owns-domain-events.md) and [ADR-0007](./decisions/0007-read-only-vs-managed-domain-events.md).

### Lifecycle

```text
            new aggregate (State.Id == default)
                        │
        ┌───────────────┴────────────────┐
        │ command path                   │ rehydration path
        ▼                                ▼
RaiseEvent(creationEvent)          LoadFromHistory(stream)
  State = State.Apply(e)             for each e:
  EnsureValidIdentity()                State = State.Apply(e)
  Version++                            EnsureValidIdentity()
  record event                         Version++
        │
        ▼
DomainEvents  (read-only, side-effect free)
        │
        ▼ (persistence collects, then — only after SaveChanges succeeds:)
((IDomainEventsManager)agg).ClearDomainEvents()
```

## Domain events

`IDomainEvent` is a pure business contract:

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }            // stable unique id (outbox / idempotency)
    DateTimeOffset OccurredAt { get; }
}
```

`DomainEvent` is a convenience base record that assigns a fresh `EventId` and derives `OccurredAt` from an injected `IClock`, keeping time deterministic and testable.

> Domain events are **internal** to a service's domain. Translating them into integration events for cross-service messaging happens at the service boundary, not here.

## Business rules and validation

Two distinct concepts, each with its own rule interface and exception:

| Concept                   | Rule interface          | Predicate     | Exception                        |
| ------------------------- | ----------------------- | ------------- | -------------------------------- |
| Invariant / business rule | `IBusinessRule`         | `IsBroken()`  | `BusinessRuleViolationException` |
| Input / domain validation | `IDomainValidationRule` | `IsInvalid()` | `DomainValidationException`      |

`RuleChecker` evaluates them:

```csharp
RuleChecker.Check(new RecipeNameMustNotBeEmpty(name)); // throws if broken
RuleChecker.Check(rule1, rule2, rule3);                // params overload, short-circuits on first failure
```

See [ADR-0009](./decisions/0009-business-rules-and-domain-validation.md).

## Usage example — one base, two styles

The aggregate carries only the command API; the state carries the apply logic.

```csharp
// State: owns Id + all apply logic. Lives in its own file, however large.
public sealed record RecipeState : IState<RecipeId>
{
    public RecipeId Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public static RecipeState Empty => new();

    public IState<RecipeId> Apply(IDomainEvent e) => e switch
    {
        RecipeCreated created => this with { Id = created.RecipeId, Name = created.Name },
        RecipeRenamed renamed => this with { Name = renamed.NewName },
        _ => this
    };
}

// Aggregate: only the public command surface. No apply noise, regardless of size.
public sealed class Recipe : AggregateRoot<RecipeId, RecipeState>
{
    private Recipe() : base(RecipeState.Empty) { }

    public static Recipe Create(RecipeId id, string name, IClock clock)
    {
        RuleChecker.Check(new RecipeNameMustNotBeEmpty(name));
        var recipe = new Recipe();
        recipe.RaiseEvent(new RecipeCreated(id, name, clock)); // first event sets State.Id
        return recipe;
    }

    public static Recipe Rehydrate(IEnumerable<IDomainEvent> history)
    {
        var recipe = new Recipe();
        recipe.LoadFromHistory(history);
        return recipe;
    }

    public void Rename(string newName, IClock clock)
    {
        RuleChecker.Check(new RecipeNameMustNotBeEmpty(newName));
        RaiseEvent(new RecipeRenamed(Id, newName, clock));
    }
}
```

- **Event-sourced** services persist the events and call `Rehydrate`/`LoadFromHistory`.
- **State-stored (EF Core)** services persist `RecipeState` (e.g. as an owned/complex type) and never replay; `Apply` and `Version` are simply unused.

## Design rules (summary)

1. The domain block has **zero** infrastructure dependencies.
2. **One aggregate base** (`AggregateRoot<TKey, TState>`) serves both ES and EF Core.
3. All **apply/evolution logic lives on the state** (`IState<TKey>`), keeping aggregates free of apply noise.
4. Aggregate **identity comes from the state**; it is validated at **every transition** (no post-creation check).
5. Equality is **identity-based** and type-sensitive on both entities and aggregates.
6. Aggregates **own** their events; outsiders read only; clearing is **explicit** and infrastructure-only.
7. Domain events are **pure** and carry an `EventId`.
8. Business rules and domain validation are **distinct**, with distinct exceptions.

## Related documents

- [ADR-0006 — Aggregate owns its domain events](./decisions/0006-aggregate-owns-domain-events.md)
- [ADR-0007 — Read-only vs. managed domain events](./decisions/0007-read-only-vs-managed-domain-events.md)
- [ADR-0008 — Entity identity and equality](./decisions/0008-entity-identity-and-equality.md)
- [ADR-0009 — Business rules and domain validation](./decisions/0009-business-rules-and-domain-validation.md)
- [ADR-0010 — Aggregate state object](./decisions/0010-aggregate-state-object.md)
- [ADR-0011 — Unified aggregate for ES and EF Core](./decisions/0011-unified-aggregate-for-es-and-ef.md)
