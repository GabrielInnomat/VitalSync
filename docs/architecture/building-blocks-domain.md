# BuildingBlocks.Domain — Technical Reference

`BuildingBlocks.Domain` is the foundational building block of the platform. It provides the tactical Domain-Driven Design primitives that every microservice's domain layer builds upon, and it is deliberately kept pure — no framework, no infrastructure, BCL only.

The block provides **two aggregate bases**: one for state-stored (EF Core) aggregates and one for event-sourced (ES) aggregates. An aggregate author chooses the base that matches the persistence strategy of the service.

> Scope: this document describes the _domain_ building block only. Application, Persistence, Infrastructure, and EventProcessing are documented separately.

## Design goals

- Provide consistent, reusable domain primitives across all services.
- Keep the domain layer **pure** — no framework, no infrastructure, BCL only.
- Make key domain rules **structural** (enforced by the type system) rather than conventional.
- Provide dedicated aggregate bases for state-stored and event-sourced services.
- Remain **independent of VitalSync** so the block is reusable in future projects.

## Contents

| Type                                        | Kind            | Responsibility                                                                       |
| ------------------------------------------- | --------------- | ------------------------------------------------------------------------------------ |
| `IEntityKey`                                | interface       | Marker contract for a strongly typed key; exposes `IsEmpty` for identity validation. |
| `IEntityKey<TValue>`                        | interface       | A strongly typed key that exposes its underlying `Value` (any `notnull` type).       |
| `IEntity<TKey>`                             | interface       | An entity with a strongly typed identity.                                            |
| `Entity<TKey>`                              | abstract class  | Base for non-aggregate entities: constructor-set identity, guard, identity equality. |
| `IState<TSelf, TKey>`                       | interface       | An aggregate's state: owns the identity and the event-apply ("evolve") logic.        |
| `IAggregateRoot<TKey>`                      | interface       | Marker for an aggregate root; exposes events **read-only**.                          |
| `IEventSourcedAggregateRoot<TKey>`          | interface       | Infrastructure-only capability exposing `Version` + `LoadFromHistory` for ES.        |
| `AggregateRoot<TKey>`                       | abstract class  | Base for **state-stored** aggregates: identity set in the constructor.               |
| `EventSourcedAggregateRoot<TKey, TState>`   | abstract class  | Base for **event-sourced** aggregates: identity derived from state via events.       |
| `IHasDomainEvents`                          | interface       | Read-only access to an aggregate's domain events.                                    |
| `IDomainEventsManager`                      | interface       | Privileged contract that can **clear** events (infrastructure-only).                 |
| `IDomainEvent`                              | interface       | Pure business event contract (`EventId`, `OccurredAt`).                              |
| `DomainEvent`                               | abstract record | Convenience base supplying `EventId` and clock-based `OccurredAt`.                   |
| `IClock`                                    | interface       | Abstraction over "now" for deterministic time.                                       |
| `IBusinessRule`                             | interface       | An invariant that can be _broken_.                                                   |
| `IDomainValidationRule`                     | interface       | A validation constraint that can be _invalid_.                                       |
| `RuleChecker`                               | static class    | Evaluates rules and throws the matching exception.                                   |
| `BusinessRuleViolationException`            | exception       | Raised when a business rule is broken.                                               |
| `DomainValidationException`                 | exception       | Raised when a domain validation rule is invalid.                                     |

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

There are two cases, by design — and they map to the two aggregate bases:

- **Non-aggregate entities** (`Entity<TKey>`) and **state-stored aggregates** (`AggregateRoot<TKey>`) receive their identity in the **constructor** and expose it get-only. The `IsEmpty` guard runs in that constructor.
- **Event-sourced aggregate roots** (`EventSourcedAggregateRoot<TKey, TState>`) take their identity from their **state** (`Id => State.Id`). A freshly created aggregate therefore starts with a default `Id`; the **first applied event** must set a non-empty identity.

### Identity validation

For `Entity<TKey>` and `AggregateRoot<TKey>` the guard runs **in the constructor**, before the id is assigned:

```csharp
protected AggregateRoot(TKey id)
{
    if (id.IsEmpty)
        throw new DomainValidationException("The id of an aggregate cannot be empty.");
    Id = id;
}
```

For `EventSourcedAggregateRoot<TKey, TState>` the identity comes from state, so the guard cannot run at construction. Instead it runs **at every state transition** — immediately after an event is applied, both in `RaiseEvent` (new behavior) and during replay in `LoadFromHistory`.

This means:

- The **first (creation) event must set a non-empty `Id`**, or the first `RaiseEvent` throws.
- **No later event may blank the `Id`** — every transition is validated.
- **Replaying a corrupt stream** that yields an empty `Id` fails immediately during rehydration.

There is deliberately **no** separate post-creation validity check on the event-sourced base; validity is intrinsic to each transition.

### Identity equality

`Entity<TKey>`, `AggregateRoot<TKey>`, and `EventSourcedAggregateRoot<TKey, TState>` all implement identity equality: two instances are equal when they are the **same concrete type** and have **equal ids**. `Equals(object?)` and `GetHashCode()` are `sealed`.

```text
left.Equals(right)  ⇔  left.GetType() == right.GetType()  ∧  left.Id == right.Id
```

> Note: two _un-created_ event-sourced aggregates both have `Id == default` and would compare equal until their creation events run. In practice aggregates are only created through factories that immediately raise the creation event.

## The state object (event sourcing)

An event-sourced aggregate's state implements the self-referencing `IState<TSelf, TKey>`:

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

### Two aggregate bases

The block provides two aggregate bases; the author picks one based on the service's persistence strategy.

**State-stored** aggregates extend `AggregateRoot<TKey>`:

```csharp
public abstract class AggregateRoot<TKey>
    : IAggregateRoot<TKey>, IDomainEventsManager, IEquatable<AggregateRoot<TKey>>
    where TKey : struct, IEntityKey
```

- Identity is passed to the constructor and validated there (`IsEmpty` guard).
- Behavior records events via `protected AddDomainEvent(...)`.
- Events are exposed **read-only**; clearing is implemented **explicitly** via `IDomainEventsManager`.
- It does **not** implement any event-sourcing capability.

**Event-sourced** aggregates extend `EventSourcedAggregateRoot<TKey, TState>`:

```csharp
public abstract class EventSourcedAggregateRoot<TKey, TState>
    : IAggregateRoot<TKey>, IDomainEventsManager,
      IEquatable<EventSourcedAggregateRoot<TKey, TState>>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
```

- It holds the current `State`, an internal version (stream position), and the private uncommitted-events list.
- `Id` is derived from `State`.
- State changes **only** via:
  - `RaiseEvent(e, clock)` — stamp the event with the clock, apply it to the state, validate identity, advance the version, and record the event (new behavior from a command);
  - `LoadFromHistory(history)` — replay a persisted stream to rebuild state (rehydration; records nothing).
- It exposes events **read-only** and implements clearing **explicitly** (see below).
- It implements `IEventSourcedAggregateRoot<TKey>` (`Version` + `LoadFromHistory`) **explicitly**, so those event-sourcing members are **not** on a concrete aggregate's public surface — a caller must deliberately cast to reach them.

| Aspect                 | `AggregateRoot<TKey>` (EF Core)      | `EventSourcedAggregateRoot<TKey, TState>` (ES) |
| ---------------------- | ------------------------------------ | ---------------------------------------------- |
| Identity source        | constructor argument                 | `State.Id`, set by the first event             |
| Persists               | the aggregate/entity object          | the raised events                              |
| Rebuilds state by      | loading the stored object            | `LoadFromHistory` (replay), via the ES cast    |
| Has `Version`          | no                                   | yes (stream position / concurrency)            |
| Records events         | `AddDomainEvent`                     | `RaiseEvent` (applies + records)               |

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
- Both aggregate bases _also_ implement `IDomainEventsManager`, but **explicitly**:

```csharp
void IDomainEventsManager.ClearDomainEvents() => _domainEvents.Clear();
```

- Raising is `protected` (`AddDomainEvent` for the state-stored base, `RaiseEvent` for the ES base), so only the aggregate itself can add events.

The same explicit-implementation technique hides the event-sourcing capability on the ES base:

```text
IEventSourcedAggregateRoot<TKey>  → long Version; void LoadFromHistory(...)   (ES infrastructure only)
```

Both `ClearDomainEvents` and the ES members are reachable **only** by code that deliberately casts to the respective interface — by convention, the persistence layer.

### Access matrix (event-sourced base)

| Caller holds…                               | Read events? | Clear events?              | Raise events?               | ES members (`Version`/`LoadFromHistory`)? |
| ------------------------------------------- | ------------ | -------------------------- | --------------------------- | ----------------------------------------- |
| The concrete aggregate (e.g. `Recipe`)      | ✅           | ❌ (not on surface)        | ❌ (only internally)        | ❌ (not on surface)                       |
| `IAggregateRoot<TKey>`                      | ✅           | ❌                         | ❌                          | ❌                                        |
| `IHasDomainEvents`                          | ✅           | ❌                         | ❌                          | ❌                                        |
| `IDomainEventsManager` (cast)               | ✅           | ✅                         | ❌                          | ❌                                        |
| `IEventSourcedAggregateRoot<TKey>` (cast)   | ✅           | ❌                         | ❌                          | ✅                                        |
| A subclass of the ES base                   | ✅           | ❌ (explicit, not visible) | ✅ (`protected RaiseEvent`) | ❌ (explicit, not visible)                |

> The state-stored base is identical except that it has no `IEventSourcedAggregateRoot<TKey>` members at all (last column is not applicable), and subclasses raise via `protected AddDomainEvent`.

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
RaiseEvent(creationEvent, clock)   LoadFromHistory(stream)
  Stamp(e, clock)                    guard: throw if uncommitted events exist
  State = State.Apply(e)             for each e:
  EnsureValidIdentity()                State = State.Apply(e)
  Version++                            Version++
  record event                       EnsureValidIdentity()  (once, after replay)
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

`DomainEvent` is a convenience base record that assigns a fresh `EventId` in its constructor and carries an `OccurredAt`. On the event-sourced base, `RaiseEvent` **stamps** the event through `IClock`: if a `DomainEvent`'s `OccurredAt` is unset (`Ticks == 0`), it is filled in with `clock.Now`, keeping time deterministic and testable.

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

## Usage example — an event-sourced aggregate

The aggregate carries only the command API; the state carries the apply logic.

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

    public static Recipe Create(RecipeId id, string name, IClock clock)
    {
        RuleChecker.Check(new RecipeNameMustNotBeEmpty(name));
        var recipe = new Recipe();
        recipe.RaiseEvent(new RecipeCreated(id, name), clock); // first event sets State.Id
        return recipe;
    }

    public void Rename(string newName, IClock clock)
    {
        RuleChecker.Check(new RecipeNameMustNotBeEmpty(newName));
        RaiseEvent(new RecipeRenamed(newName), clock);
    }
}
```

- **Event-sourced** services persist the events and rehydrate through the ES view:

  ```csharp
  var recipe = new Recipe();
  ((IEventSourcedAggregateRoot<RecipeId>)recipe).LoadFromHistory(history);
  ```

  (In practice this cast lives inside the event-sourced repository, not in domain or application code.)

## Usage example — a state-stored aggregate

State-stored aggregates take their identity in the constructor and record behavior with `AddDomainEvent`:

```csharp
public sealed class Recipe : AggregateRoot<RecipeId>
{
    private Recipe(RecipeId id, string name) : base(id) // base runs the IsEmpty guard
    {
        Name = name;
    }

    public string Name { get; private set; }

    public static Recipe Create(RecipeId id, string name)
    {
        RuleChecker.Check(new RecipeNameMustNotBeEmpty(name));
        var recipe = new Recipe(id, name);
        recipe.AddDomainEvent(new RecipeCreated(id, name));
        return recipe;
    }
}
```

The persistence layer stores the object directly (e.g. via EF Core); `Version`, `Apply`, and `LoadFromHistory` do not exist on this base.

## Design rules (summary)

1. The domain block has **zero** infrastructure dependencies.
2. **Two aggregate bases** are provided: `AggregateRoot<TKey>` (state-stored) and `EventSourcedAggregateRoot<TKey, TState>` (event-sourced). The author picks the base that matches the service's persistence strategy.
3. For event-sourced aggregates, all **apply/evolution logic lives on the state** (`IState<TSelf, TKey>`), keeping aggregates free of apply noise.
4. **Identity validation is type-agnostic**, driven by `IEntityKey.IsEmpty`. State-stored aggregates/entities validate in the constructor; event-sourced aggregates validate at **every transition** (no post-creation check).
5. Equality is **identity-based** and type-sensitive on both entities and aggregates.
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
- [ADR-0011 — Unified aggregate for ES and EF Core](./decisions/0011-unified-aggregate-for-es-and-ef.md)
