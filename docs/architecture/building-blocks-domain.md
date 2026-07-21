# BuildingBlocks.Domain — Technical Reference

`BuildingBlocks.Domain` is the foundational building block of the platform. It provides the tactical Domain-Driven Design primitives that every microservice's domain layer builds upon, and it is deliberately free of any framework or infrastructure dependency.

A central design goal of this block is that **a single aggregate model serves both persistence worlds** — event sourcing (ES) and state-stored (EF Core) — so that the choice of persistence strategy is an infrastructure decision made in the Application/Persistence layer, never a decision baked into the domain.

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
| `IEventSourcedAggregateRoot<TKey>` | interface       | Infrastructure-only capability exposing `Version` + `LoadFromHistory` for ES.        |
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
- **Aggregate roots** (`AggregateRoot<TKey, TState>`) take their identity from their **state** (`Id => State.Id`). A freshly created aggregate therefore starts with a default `Id`; the **first applied event** must set it.

### Identity validation for aggregates

Because an aggregate's identity comes from its state, the empty-GUID guard cannot run at construction. Instead it runs **at every state transition**: immediately after an event is applied (in both `RaiseEvent` and replay).

This means:

- The **first (creation) event must set a non-empty `Id`**, or the first `RaiseEvent` throws.
- **No later event may blank the `Id`** — every transition is validated.
- **Replaying a corrupt stream** that yields an empty `Id` fails immediately during rehydration.

There is deliberately **no** separate post-creation validity check; validity is intrinsic to each transition.

### Identity equality

Both `Entity<TKey>` and `AggregateRoot<TKey, TState>` implement identity equality: two instances are equal when they are the **same concrete type** and have **equal ids**. `Equals(object?)` and `GetHashCode()` are `sealed`.

```text
left.Equals(right)  ⇔  left.GetType() == right.GetType()  ∧  left.Id == right.Id
```

> Note: two _un-created_ aggregates both have `Id == default` and would compare equal until their creation events run. In practice aggregates are only created through factories that immediately raise the creation event.

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

The state object exists to keep large aggregates maintainable. All **apply/evolution logic lives on the state**, so the aggregate class contains only the public command API (the behavior invoked by commands).

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
    : IAggregateRoot<TKey>, IDomainEventsManager,
      IEquatable<AggregateRoot<TKey, TState>>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TKey>
```

- It holds the current `State`, an internal version (stream position), and the private uncommitted-events list.
- `Id` is derived from `State`.
- State changes **only** via:
  - `RaiseEvent(e)` — apply the event to the state, validate identity, advance the version, and record the event (new behavior from a command);
  - `LoadFromHistory(history)` — replay a persisted stream to rebuild state (rehydration; records nothing).
- It exposes events **read-only** and implements clearing **explicitly** (see below).
- It implements `IEventSourcedAggregateRoot<TKey>` (`Version` + `LoadFromHistory`) **explicitly**, so those event-sourcing members are **not** on a concrete aggregate's public surface — a caller must cast to `IEventSourcedAggregateRoot<TKey>` to reach them (exactly like `IDomainEventsManager.ClearDomainEvents()`).

How each world uses it:

|                        | Event-sourced                                        | State-stored (EF Core)               |
| ---------------------- | ---------------------------------------------------- | ------------------------------------ |
| Persists               | the raised events                                    | the `TState` object                  |
| Rebuilds state by      | `LoadFromHistory` (replay), via the ES cast          | loading the stored `TState`          |
| Uses `Version`         | yes (stream position / concurrency), via the ES cast | never obtains the ES view; invisible |
| Uses `Apply`           | yes (replay + new events)                            | inert (state is loaded directly)     |
| Sees ES members public | via `IEventSourcedAggregateRoot<TKey>` cast only     | never                                |

The aggregate author does **not** choose a base class per strategy; they always use `AggregateRoot<TKey, TState>`. **The ES-vs-EF decision is made in the Application/Persistence layer** by selecting the appropriate repository for the context. See [ADR-0011](./decisions/0011-unified-aggregate-for-es-and-ef.md).

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

The same explicit-implementation technique hides the event-sourcing capability:

```text
IEventSourcedAggregateRoot<TKey>  → long Version; void LoadFromHistory(...)   (ES infrastructure only)
```

Both `ClearDomainEvents` and the ES members are reachable **only** by code that deliberately casts to the respective interface — by convention, the persistence layer.

### Access matrix

| Caller holds…                               | Read events? | Clear events?              | Raise events?               | ES members (`Version`/`LoadFromHistory`)? |
| ------------------------------------------- | ------------ | -------------------------- | --------------------------- | ----------------------------------------- |
| The concrete aggregate (e.g. `Recipe`)      | ✅           | ❌ (not on surface)        | ❌ (only internally)        | ❌ (not on surface)                       |
| `IAggregateRoot<TKey>`                      | ✅           | ❌                         | ❌                          | ❌                                        |
| `IHasDomainEvents`                          | ✅           | ❌                         | ❌                          | ❌                                        |
| `IDomainEventsManager` (cast)               | ✅           | ✅                         | ❌                          | ❌                                        |
| `IEventSourcedAggregateRoot<TKey>` (cast)   | ✅           | ❌                         | ❌                          | ✅                                        |
| A subclass of `AggregateRoot<TKey, TState>` | ✅           | ❌ (explicit, not visible) | ✅ (`protected RaiseEvent`) | ❌ (explicit, not visible)                |

### Why clearing (and ES capability) is separated

The persistence layer collects events, dispatches them, and clears them **only after a successful save**. Keeping `ClearDomainEvents()` off the public surface prevents any application layer from clearing prematurely (which would silently drop undispatched events). The same reasoning applies to `Version`/`LoadFromHistory`: a state-stored aggregate should never see event-sourcing ceremony, and only event-sourcing infrastructure that deliberately casts to `IEventSourcedAggregateRoot<TKey>` may rehydrate. See [ADR-0006](./decisions/0006-aggregate-owns-domain-events.md), [ADR-0007](./decisions/0007-read-only-vs-managed-domain-events.md), and [ADR-0011](./decisions/0011-unified-aggregate-for-es-and-ef.md).

### Replay-misuse guard

`LoadFromHistory` may only rebuild a **fresh** aggregate. If it is called on an aggregate that already has state (its internal version is `> 0` — because events were raised or a prior replay ran), it throws a `DomainValidationException`. This turns accidental rehydration of an already-materialized aggregate (e.g. an EF-Core repository mistakenly replaying) into an immediate, obvious failure instead of silent state corruption.

### Lifecycle

```text
            new aggregate (State.Id == default)
                        │
        ┌───────────────┴────────────────┐
        │ command path                   │ rehydration path (ES infra casts to
        ▼                                ▼  IEventSourcedAggregateRoot<TKey>)
RaiseEvent(creationEvent)          LoadFromHistory(stream)
  State = State.Apply(e)             guard: throw if version > 0
  EnsureValidIdentity()              for each e:
  Version++                            State = State.Apply(e)
  record event                         EnsureValidIdentity()
        │                               Version++
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

// Aggregate: only the public command surface. No apply noise, no ES ceremony.
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

    public void Rename(string newName, IClock clock)
    {
        RuleChecker.Check(new RecipeNameMustNotBeEmpty(newName));
        RaiseEvent(new RecipeRenamed(Id, newName, clock));
    }
}
```

- **Event-sourced** services persist the events and rehydrate through the ES view:

  ```csharp
  var recipe = new Recipe();
  ((IEventSourcedAggregateRoot<RecipeId>)recipe).LoadFromHistory(history);
  ```

  (In practice this cast lives inside the event-sourced repository, not in domain or application code.)
- **State-stored (EF Core)** services persist `RecipeState` (e.g. as an owned/complex type) and never replay; `Apply`, `Version`, and `LoadFromHistory` are simply never reached.

## Design rules (summary)

1. The domain block has **zero** infrastructure dependencies.
2. **One aggregate base** (`AggregateRoot<TKey, TState>`) serves both ES and EF Core; the persistence choice is made in the Application/Persistence layer, not by the aggregate's type.
3. All **apply/evolution logic lives on the state** (`IState<TKey>`), keeping aggregates free of apply noise.
4. Aggregate **identity comes from the state**; it is validated at **every transition** (no post-creation check).
5. Equality is **identity-based** and type-sensitive on both entities and aggregates.
6. Aggregates **own** their events; outsiders read only; clearing is **explicit** and infrastructure-only.
7. **Event-sourcing capability** (`Version`/`LoadFromHistory`) is exposed via **explicit** `IEventSourcedAggregateRoot<TKey>` implementation — off the public surface — and `LoadFromHistory` is guarded against replay onto an already-materialized aggregate.
8. Domain events are **pure** and carry an `EventId`.
9. Business rules and domain validation are **distinct**, with distinct exceptions.

## Related documents

- [ADR-0006 — Aggregate owns its domain events](./decisions/0006-aggregate-owns-domain-events.md)
- [ADR-0007 — Read-only vs. managed domain events](./decisions/0007-read-only-vs-managed-domain-events.md)
- [ADR-0008 — Entity identity and equality](./decisions/0008-entity-identity-and-equality.md)
- [ADR-0009 — Business rules and domain validation](./decisions/0009-business-rules-and-domain-validation.md)
- [ADR-0010 — Aggregate state object](./decisions/0010-aggregate-state-object.md)
- [ADR-0011 — Unified aggregate for ES and EF Core](./decisions/0011-unified-aggregate-for-es-and-ef.md)
