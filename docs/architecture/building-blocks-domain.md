# BuildingBlocks.Domain — Technical Reference

`BuildingBlocks.Domain` is the foundational building block of the platform. It provides the tactical Domain-Driven Design primitives that every microservice's domain layer builds upon, and it is deliberately kept pure — no framework, no infrastructure, BCL only.

The block provides a **single aggregate authoring model**: every aggregate derives from `AggregateRoot<TKey, TState>` and expresses state changes by folding domain events into an immutable state object. Event sourcing is a purely **additive** capability — `EventSourcedAggregateRoot<TKey, TState>` adds only a version and replay — so how an aggregate is persisted is a composition-layer decision, not a class-hierarchy decision (see [ADR-0025](./decisions/0025-unified-state-fold-aggregate-model.md)).

> Scope: this document describes the _domain_ building block only. Application, and Infrastructure are documented separately.

## Design goals

- Provide consistent, reusable domain primitives across all services.
- Keep the domain layer **pure** — no framework, no infrastructure, BCL only.
- Make key domain rules **structural** (enforced by the type system) rather than conventional.
- Provide dedicated aggregate bases for state-stored and event-sourced services, sharing one authoring model.
- Remain **independent of VitalSync** so the block is reusable in future projects.

## Contents

| Type                                      | Kind            | Responsibility                                                                       |
| ----------------------------------------- | --------------- | ------------------------------------------------------------------------------------ |
| `IEntityKey`                              | interface       | Marker contract for a strongly typed key; exposes `IsEmpty` for identity validation. |
| `IEntityKey<TValue>`                      | interface       | A strongly typed key that exposes its underlying `Value` (any `notnull` type).       |
| `IEntity<TKey>`                           | interface       | An entity with a strongly typed identity.                                            |
| `EntityBase<TKey>`                        | abstract class  | Shared identity-equality base for entities and aggregate roots (single equality implementation). |
| `Entity<TKey>`                            | abstract class  | Base for non-aggregate entities: constructor-set identity, guard, identity equality. |
| `IState<TSelf, TKey>`                     | interface       | An aggregate's state: owns the identity and the event-apply ("evolve") logic.        |
| `IAggregateRoot<TKey>`                    | interface       | Marker for an aggregate root; exposes events **read-only**.                          |
| `IEventSourcedAggregateRoot<TKey>`        | interface       | Infrastructure-only capability exposing `Version` + `LoadFromHistory` for ES.        |
| `AggregateRoot<TKey, TState>`             | abstract class  | The single aggregate base: state fold via `RaiseEvent`, identity derived from state. |
| `EventSourcedAggregateRoot<TKey, TState>` | abstract class  | Additive base for **event-sourced** aggregates: adds `Version` + `LoadFromHistory`.  |
| `IHasDomainEvents`                        | interface       | Read-only access to an aggregate's domain events.                                    |
| `IDomainEventsManager`                    | interface       | Privileged contract that can **clear** events (infrastructure-only).                 |
| `IDomainEvent`                            | interface       | Pure business event contract (`EventId`, `OccurredAt`).                              |
| `DomainEvent`                             | abstract record | Convenience base supplying `EventId` and clock-based `OccurredAt`.                   |
| `IClock`                                  | interface       | Abstraction over "now" for deterministic time.                                       |
| `IBusinessRule`                           | interface       | An invariant that can be _broken_.                                                   |
| `IDomainValidationRule`                   | interface       | A validation constraint that can be _invalid_.                                       |
| `RuleChecker`                             | static class    | Evaluates rules and throws the matching exception.                                   |
| `BusinessRuleViolationException`          | exception       | Raised when a business rule is broken.                                               |
| `DomainValidationException`               | exception       | Raised when a domain validation rule is invalid.                                     |

## Identity and keys

### Strongly typed keys

Keys are modeled with **two interfaces**:

```csharp
public interface IEntityKey
{
    bool IsEmpty { get; }
}

public interface IEntityKey<out TValue> : IEntityKey
    where TValue : notnull
{
    TValue Value { get; }
}
```

- `IEntityKey` is the **non-generic marker**. It is the constraint used throughout the block (`where TKey : struct, IEntityKey`), so the base classes can stay agnostic of the underlying value type. It also declares `IsEmpty`, which each key implements to define what "empty/invalid" means for its own value type.
- `IEntityKey<TValue>` exposes the underlying primitive via `Value`. The value type can be **any `notnull` type** — `Guid`, `int`, `string`, etc. — not just `Guid`.

Every aggregate/entity key is a `readonly record struct` implementing `IEntityKey<TValue>` and providing an `IsEmpty` rule:

```csharp
public readonly record struct RecipeId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;
}
```

Because each aggregate has its own key type, passing the wrong key is a **compile-time error**: a `RecipeId` is not an `IngredientId`, even though both wrap a `Guid`. See [ADR-0008](./decisions/0008-entity-identity-and-equality.md).

> Because `IsEmpty` lives on the key, identity validation is **type-agnostic**: an `int`-backed key can define `IsEmpty => Value <= 0`, a `string`-backed key `IsEmpty => string.IsNullOrWhiteSpace(Value)`, and so on. The base classes never inspect the raw value type themselves.

### Where identity lives

There are two cases, by design:

- **Non-aggregate entities** (`Entity<TKey>`) receive their identity in the **constructor** and expose it get-only. The `IsEmpty` guard runs in that constructor.
- **Aggregate roots** (`AggregateRoot<TKey, TState>` and its event-sourced derivative) take their identity from their **state** (`Id => State.Id`). A freshly created aggregate therefore starts with a default `Id`; the **first applied event** must set a non-empty identity.

### Identity validation

For `Entity<TKey>` the guard runs **in the constructor**, before the id is assigned:

```csharp
protected Entity(TKey id)
{
    if (id.IsEmpty)
        throw new DomainValidationException("The id of an entity cannot be empty.");
    Id = id;
}
```

For aggregates the identity comes from state, so the guard cannot run at construction. Instead it runs **at every state transition** — immediately after an event is applied, both in `RaiseEvent` (new behavior) and during replay in `LoadFromHistory`.

This means:

- The **first (creation) event must set a non-empty `Id`**, or the first `RaiseEvent` throws.
- **No later event may blank the `Id`** — every transition is validated.
- **Replaying a corrupt stream** that yields an empty `Id` fails immediately during rehydration.

There is deliberately **no** separate post-creation validity check on the aggregate bases; validity is intrinsic to each transition.

### Identity equality

The identity-equality implementation lives **once**, on `EntityBase<TKey>`, from which both `Entity<TKey>` and `AggregateRoot<TKey, TState>` derive: two instances are equal when they are the **same concrete type** and have **equal ids**. `Equals(object?)` and `GetHashCode()` are `sealed`. `EntityBase<TKey>`'s constructor is `private protected`, so domain code always derives from `Entity<TKey>` or an aggregate base, never from it directly.

```text
left.Equals(right)  ⇔  left.GetType() == right.GetType()  ∧  left.Id == right.Id
```

> Note: two _un-created_ aggregates both have `Id == default` and would compare equal until their creation events run. In practice aggregates are only created through factories that immediately raise the creation event.

## The state object

Every aggregate's state implements the self-referencing `IState<TSelf, TKey>`:

```csharp
public interface IState<TSelf, out TKey>
    where TSelf : IState<TSelf, TKey>
    where TKey : struct, IEntityKey
{
    TKey Id { get; }                    // the aggregate's identity
    TSelf Apply(IDomainEvent domainEvent); // the evolve/apply function, returns the next state
}
```

The `TSelf` type parameter lets `Apply` return the **concrete** state type rather than the interface, so no casting is needed when evolving state.

The state object exists to keep large aggregates maintainable. All **apply/evolution logic lives on the state**, so the aggregate class contains only the public command API (the behavior invoked by callers).

State implementations are expected to be **immutable**: `Apply` returns the next state (`this with { … }`) rather than mutating in place.

```csharp
public sealed record RecipeState(RecipeId Id, string Name)
    : IState<RecipeState, RecipeId>
{
    public static RecipeState Empty => new(default, string.Empty);

    public RecipeState Apply(IDomainEvent e) => e switch
    {
        RecipeCreated created => this with { Id = created.RecipeId, Name = created.Name },
        RecipeRenamed renamed => this with { Name = renamed.NewName },
        _ => this
    };
}
```

## Aggregates and domain events

### One authoring model, two bases

Every aggregate derives (directly or indirectly) from the single base `AggregateRoot<TKey, TState>`:

```csharp
public abstract class AggregateRoot<TKey, TState>
    : EntityBase<TKey>, IAggregateRoot<TKey>, IDomainEventsManager
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
```

- It holds the current immutable `protected TState State` and the private uncommitted-events list.
- `Id` is derived from `State`.
- State changes **only** via `protected RaiseEvent(e)` — apply the event to the state, validate identity, and record the event. There is no other mutation path; the state fold forces every change to be expressed as an event.
- Events are exposed **read-only**; clearing is implemented **explicitly** via `IDomainEventsManager`.

**Event-sourced** aggregates extend `EventSourcedAggregateRoot<TKey, TState>`, which is purely **additive**:

```csharp
public abstract class EventSourcedAggregateRoot<TKey, TState>
    : AggregateRoot<TKey, TState>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
```

- It adds an internal version (stream position) that advances on every raise/replay.
- It adds `LoadFromHistory(history)` — replay a persisted stream to rebuild state (rehydration; records nothing).
- It implements `IEventSourcedAggregateRoot<TKey>` (`Version` + `LoadFromHistory`) **explicitly**, so those event-sourcing members are **not** on a concrete aggregate's public surface — a caller must deliberately cast to reach them.

Choosing the event-sourced base is a statement that the aggregate's **event history itself carries business value**. Because the authoring model is identical, moving an aggregate between the two worlds is a base-class change plus a composition-layer change — the business logic, state type, and tests stay untouched ([ADR-0025](./decisions/0025-unified-state-fold-aggregate-model.md)).

| Aspect            | `AggregateRoot<TKey, TState>` (state-stored) | `EventSourcedAggregateRoot<TKey, TState>` (ES) |
| ----------------- | -------------------------------------------- | ---------------------------------------------- |
| Identity source   | `State.Id`, set by the first event           | `State.Id`, set by the first event             |
| Persists          | the current state                            | the raised events                              |
| Rebuilds state by | loading the stored state                     | `LoadFromHistory` (replay), via the ES cast    |
| Has `Version`     | no                                           | yes (stream position / concurrency)            |
| Records events    | `RaiseEvent` (applies + records)             | `RaiseEvent` (applies + records)               |

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
- The aggregate base _also_ implements `IDomainEventsManager`, but **explicitly**:

```csharp
void IDomainEventsManager.ClearDomainEvents() => _domainEvents.Clear();
```

- Raising (`RaiseEvent`) is `protected`, so only the aggregate itself can add events.

The same explicit-implementation technique hides the event-sourcing capability on the ES base:

```text
IEventSourcedAggregateRoot<TKey>  → long Version; void LoadFromHistory(...)   (ES infrastructure only)
```

Both `ClearDomainEvents` and the ES members are reachable **only** by code that deliberately casts to the respective interface — by convention, the persistence layer.

### Access matrix (event-sourced base)

| Caller holds…                             | Read events? | Clear events?              | Raise events?               | ES members (`Version`/`LoadFromHistory`)? |
| ----------------------------------------- | ------------ | -------------------------- | --------------------------- | ----------------------------------------- |
| The concrete aggregate (e.g. `Recipe`)    | ✅           | ❌ (not on surface)        | ❌ (only internally)        | ❌ (not on surface)                       |
| `IAggregateRoot<TKey>`                    | ✅           | ❌                         | ❌                          | ❌                                        |
| `IHasDomainEvents`                        | ✅           | ❌                         | ❌                          | ❌                                        |
| `IDomainEventsManager` (cast)             | ✅           | ✅                         | ❌                          | ❌                                        |
| `IEventSourcedAggregateRoot<TKey>` (cast) | ✅           | ❌                         | ❌                          | ✅                                        |
| A subclass of the ES base                 | ✅           | ❌ (explicit, not visible) | ✅ (`protected RaiseEvent`) | ❌ (explicit, not visible)                |

> The state-stored base is identical except that it has no `IEventSourcedAggregateRoot<TKey>` members at all (last column is not applicable).

### Why clearing (and ES capability) is separated

The persistence layer collects events, dispatches them, and clears them **only after a successful save**. Keeping `ClearDomainEvents()` off the public surface prevents any application layer from silently dropping events.

### Replay-misuse guard

`LoadFromHistory` may only rebuild an aggregate that has **not yet raised any uncommitted events**. If it is called after events were raised via `RaiseEvent`, it throws an `InvalidOperationException`:

```csharp
if (_domainEvents.Count > 0)
    throw new InvalidOperationException(
        "LoadFromHistory cannot be called after events have been raised on the aggregate.");
```

The guard checks for **pending domain events** rather than `Version > 0` **on purpose**. Snapshot-based rehydration restores state and version from a snapshot (which raises no domain events) and then replays only the events _after_ the snapshot. A `Version > 0` guard would make that impossible; the uncommitted-events guard still catches the real misuse (replaying history onto an aggregate that already has command-produced, unsaved state) while leaving snapshotting open as a future capability.

### Lifecycle (event-sourced)

```text
            new aggregate (State.Id == default)
                        │
        ┌───────────────┴────────────────┐
        │ command path                   │ rehydration path (ES infra casts to
        ▼                                ▼  IEventSourcedAggregateRoot<TKey>)
RaiseEvent(creationEvent)          LoadFromHistory(stream)
  State = State.Apply(e)             guard: throw if uncommitted events exist
  EnsureValidIdentity()              for each e:
  record event                         State = State.Apply(e)
  Version++  (ES base only)            EnsureValidIdentity()
        │                              Version++
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

`DomainEvent` is a convenience base record that assigns a fresh `EventId` in its constructor and carries an `OccurredAt`. Events are **pure data**: `RaiseEvent` does not stamp them — if a `DomainEvent`'s `OccurredAt` is unset, it is filled in at commit time by the infrastructure's stamper (via `IClock`), keeping time deterministic and testable.

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

## Usage example — an aggregate

The aggregate carries only the command API; the state carries the apply logic. The same code works for a state-stored aggregate (base `AggregateRoot<RecipeId, RecipeState>`) and an event-sourced one (base `EventSourcedAggregateRoot<RecipeId, RecipeState>`).

```csharp
// Key: defines its own emptiness rule.
public readonly record struct RecipeId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;
}

// State: owns Id + all apply logic. Lives in its own file, however large.
public sealed record RecipeState(RecipeId Id, string Name)
    : IState<RecipeState, RecipeId>
{
    public static RecipeState Empty => new(default, string.Empty);

    public RecipeState Apply(IDomainEvent e) => e switch
    {
        RecipeCreated created => this with { Id = created.RecipeId, Name = created.Name },
        RecipeRenamed renamed => this with { Name = renamed.NewName },
        _ => this
    };
}

// Aggregate: only the public command surface. No apply noise, no ES ceremony.
public sealed class Recipe : EventSourcedAggregateRoot<RecipeId, RecipeState>
{
    private Recipe() : base(RecipeState.Empty) { }

    public static Recipe Create(RecipeId id, string name)
    {
        RuleChecker.Check(new RecipeNameMustNotBeEmpty(name));
        var recipe = new Recipe();
        recipe.RaiseEvent(new RecipeCreated(id, name)); // first event sets State.Id
        return recipe;
    }

    public void Rename(string newName)
    {
        RuleChecker.Check(new RecipeNameMustNotBeEmpty(newName));
        RaiseEvent(new RecipeRenamed(newName));
    }
}
```

- **Event-sourced** services persist the events and rehydrate through the ES view:

    ```csharp
    var recipe = new Recipe();
    ((IEventSourcedAggregateRoot<RecipeId>)recipe).LoadFromHistory(history);
    ```

    (In practice this cast lives inside the event-sourced repository, not in domain or application code.)

- **State-stored** services persist the `State` record directly (e.g. via EF Core) and rehydrate by constructing the aggregate from the stored state; `Version` and `LoadFromHistory` do not exist on the state-stored base.

Switching this aggregate between the two worlds means changing its base class and the persistence registration in the composition layer — nothing else.

## Design rules (summary)

1. The domain block has **zero** infrastructure dependencies.
2. **One authoring model**: every aggregate derives from `AggregateRoot<TKey, TState>` and changes state only through `RaiseEvent`; `EventSourcedAggregateRoot<TKey, TState>` merely adds `Version` + `LoadFromHistory` when the event history carries business value.
3. All **apply/evolution logic lives on the state** (`IState<TSelf, TKey>`), keeping aggregates free of apply noise.
4. **Identity validation is type-agnostic**, driven by `IEntityKey.IsEmpty`. Entities validate in the constructor; aggregates validate at **every transition** (no post-creation check).
5. Equality is **identity-based** and type-sensitive, implemented once on `EntityBase<TKey>`.
6. Aggregates **own** their events; outsiders read only; clearing is **explicit** and infrastructure-only.
7. **Event-sourcing capability** (`Version`/`LoadFromHistory`) is exposed via **explicit** `IEventSourcedAggregateRoot<TKey>` implementation — off the public surface — and `LoadFromHistory` is guarded against replay misuse (throws if uncommitted events exist), which keeps snapshotting possible.
8. Domain events are **pure** and carry an `EventId`.
9. Business rules and domain validation are **distinct**, with distinct exceptions.

## Related documents

- [ADR-0006 — Aggregate owns its domain events](./decisions/0006-aggregate-owns-domain-events.md)
- [ADR-0007 — Read-only vs. managed domain events](./decisions/0007-read-only-vs-managed-domain-events.md)
- [ADR-0008 — Entity identity and equality](./decisions/0008-entity-identity-and-equality.md)
- [ADR-0009 — Business rules and domain validation](./decisions/0009-business-rules-and-domain-validation.md)
- [ADR-0010 — Aggregate state object](./decisions/0010-aggregate-state-object.md)
- [ADR-0025 — Unified state-fold aggregate model with additive event sourcing](./decisions/0025-unified-state-fold-aggregate-model.md)
- [ADR-0026 — Single repository contract](./decisions/0026-single-repository-contract.md)

