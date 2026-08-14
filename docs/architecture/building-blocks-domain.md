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
| `Entity<TKey, TState>`                    | abstract class  | The base for a **non-aggregate entity**, always a child inside an aggregate: constructor-set identity with guard, identity equality, its own state read through the root, and `RaiseEvent` via the root's channel. |
| `EntityState<TSelf, TKey>`                | abstract record | A child entity's state: identity plus a pure `Apply`. Carries **no** version.        |
| `IDomainEventRaiser`                      | interface       | The aggregate-internal channel a child raises through (explicit, never called by application code). |
| `AggregateState<TSelf, TKey>`             | abstract record | An aggregate's state: owns the identity, the version, and the event-apply ("evolve") logic. |
| `IAggregateRoot<TKey>`                    | interface       | Marker for an aggregate root; exposes events **read-only**.                          |
| `IEventSourcedAggregateRoot<TKey>`        | interface       | Infrastructure-only capability exposing `LoadFromHistory` for ES replay.               |
| `AggregateRoot<TKey, TState>`             | abstract class  | The single aggregate base: state fold via `RaiseEvent`, identity derived from state. |
| `EventSourcedAggregateRoot<TKey, TState>` | abstract class  | Additive base for **event-sourced** aggregates: adds `LoadFromHistory`, nothing else. |
| `IHasDomainEvents`                        | interface       | Read-only access to an aggregate's domain events.                                    |
| `IDomainEventOwner`                       | interface       | Privileged contract that can **clear** events (infrastructure-only, explicit).       |
| `IStateOwner`                             | interface       | Privileged access to the aggregate's state object **and its `Version`** (infrastructure-only, explicit). |
| `IDomainEvent`                            | interface       | Pure business event marker — no identity fields (ADR-0029).                          |
| `DomainEvent`                             | abstract record | Convenience base; identity travels on the envelope, not the event.                   |
| `IClock`                                  | interface       | Abstraction over "now" for deterministic time.                                       |
| `IBusinessRule`                           | interface       | An invariant that can be _broken_.                                                   |
| `IDomainValidationRule`                   | interface       | A validation constraint that can be _invalid_.                                       |
| `RuleChecker`                             | static class    | Evaluates rules and throws the matching exception; a `null` rule throws.           |
| `KebabCase`                               | static class    | The single kebab-case validator, shared by persisted names and integration-event topics. |
| `BusinessRuleViolationException`          | exception       | Raised when a business rule is broken.                                               |
| `DomainValidationException`               | exception       | Raised when a domain validation rule is invalid.                                     |

## Folder layout — folder is namespace, and it is a contract

```
BuildingBlocks.Domain/
├── Aggregates/   AggregateRoot, AggregateState, EventSourcedAggregateRoot, IAggregateRoot,
│                 IEventSourcedAggregateRoot, IStateOwner
├── Entities/     IEntityKey, IEntity, EntityBase, Entity, EntityState
├── Events/       IDomainEvent, DomainEvent, IHasDomainEvents, IDomainEventOwner, IDomainEventRaiser
├── Naming/       EventNameAttribute, AggregateNameAttribute, KebabCase, ContractName (internal)
├── Rules/        IBusinessRule, IDomainValidationRule, RuleChecker, both exceptions
└── IClock.cs     stays in the root — it belongs to no group and serves all of them
```

The cut follows the aggregate boundary, not an abstract taxonomy: `Aggregates/` holds what a root
is, `Entities/` what a root and a child share, and `Events/` the channel between them. The root
namespace `BuildingBlocks.Domain` is deliberately almost empty.

Because every type here is `public`, **the namespaces are part of the published API**: moving a
file changes each exported type's `FullName` and breaks every consumer. `PublicSurfaceTests` pins
the full list of exported type names, so a move fails a test instead of a downstream build.

A service does **not** pay for this per file. The four to five usings go into the `.csproj` once:

```xml
<ItemGroup>
  <Using Include="BuildingBlocks.Domain.Aggregates" />
  <Using Include="BuildingBlocks.Domain.Entities" />
  <Using Include="BuildingBlocks.Domain.Events" />
  <Using Include="BuildingBlocks.Domain.Naming" />
  <Using Include="BuildingBlocks.Domain.Rules" />
</ItemGroup>
```

Both sample domain projects do exactly this; `Widget.cs` carries no using directive at all.

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

- `IEntityKey` is the **non-generic marker**. It is the constraint used throughout the block (`where TKey : struct, IEntityKey, IEquatable<TKey>`), so the base classes can stay agnostic of the underlying value type. It also declares `IsEmpty`, which each key implements to define what "empty/invalid" means for its own value type.
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

> `IsEmpty` stays a **domain** concept and never reaches storage. A typed key serializes as its bare underlying value (`"RecipeId": "8f3a…"`), not as an object with `Value` and `IsEmpty`; the converter that guarantees it lives in `Infrastructure`, so the key type needs no serialization attribute and the domain needs no serializer dependency. See [ADR-0034](./decisions/0034-typed-keys-serialize-as-bare-values.md).

### Where identity lives

There are two cases, by design:

- **Non-aggregate entities** (`Entity<TKey, TState>`) receive their identity in the **constructor** and expose it get-only. The `IsEmpty` guard runs in that constructor.
- **Aggregate roots** (`AggregateRoot<TKey, TState>` and its event-sourced derivative) take their identity from their **state** (`Id => State.Id`). A freshly created aggregate therefore starts with a default `Id`; the **first applied event** must set a non-empty identity.

### Identity validation

For `Entity<TKey, TState>` the guard runs **in the constructor**, before the id is assigned:

```csharp
protected Entity(IDomainEventRaiser raiser, TKey id, Func<TKey, TState?> stateLookup)
{
    if (id.IsEmpty)
        throw new DomainValidationException("The id of an entity cannot be empty.");
    ArgumentNullException.ThrowIfNull(raiser);
    ArgumentNullException.ThrowIfNull(stateLookup);
    Id = id;
    …
}
```

For aggregates the identity comes from state, so the guard cannot run at construction. Instead it runs **at every state transition** — immediately after an event is applied, both in `RaiseEvent` (new behavior) and during replay in `LoadFromHistory`.

This means:

- The **first (creation) event must set a non-empty `Id`**, or the first `RaiseEvent` throws.
- **No later event may blank the `Id`** — every transition is validated.
- **Replaying a corrupt stream** that yields an empty `Id` fails immediately during rehydration.

There is deliberately **no** separate post-creation validity check on the aggregate bases; validity is intrinsic to each transition.

### Identity equality

The identity-equality implementation lives **once**, on `EntityBase<TKey>`, which has exactly two children — `Entity<TKey, TState>` and `AggregateRoot<TKey, TState>`: two instances are equal when they are the **same concrete type** and have **equal ids**. `Equals(object?)` and `GetHashCode()` are `sealed`. `EntityBase<TKey>`'s constructor is `private protected`, so domain code always derives from `Entity<TKey, TState>` or an aggregate base, never from it directly.

```text
left.Equals(right)  ⇔  left.GetType() == right.GetType()  ∧  left.Id == right.Id
```

> Note: two _un-created_ aggregates both have `Id == default` and would compare equal until their creation events run. That is left as-is on purpose — see below.

#### Why the equality rule does not special-case an empty identity

A hull without identity never leaves the domain, because **four guards** stand in the way:

| Guard | Where | Rejects |
| --- | --- | --- |
| Constructor check | `Entity<TKey, TState>` | a child constructed with an empty id |
| Fold check | `AggregateRoot.ApplyEvent` | a state whose id is still empty after applying an event |
| Restore check | `IStateOwner.Restore` | a persisted state carrying an empty identity |
| Repository check | `EfCoreRepository` / `MartenEventSourcedRepository` | `AddAsync` with an empty identity |

What remains is the window between an aggregate's private parameterless constructor and its first `RaiseEvent` — two statements on a local variable, inside a named factory or inside `AggregateFactory`. Nothing reachable from application code.

Guarding inside `Equals` instead was rejected: a clause like `!Id.IsEmpty && …` makes an object unequal **to itself**, so it needs a `ReferenceEquals` short-circuit to stay reflexive — real complexity in the hottest equality path, for a hole that has no exit. `TransientIdentityTests` pins the arrangement, including the corollary that a hull's hash code **changes** when it gains identity, so a hull must never be put into a `HashSet` or used as a dictionary key.

#### A key must declare value equality

The constraint is `where TKey : struct, IEntityKey, IEquatable<TKey>`. Without the second interface, `Id.Equals(other.Id)` binds to `ValueType.Equals(object)`: field comparison by reflection, plus a boxing allocation on every comparison. With it, the typed overload is visible, the boxing disappears, and a key that has no value equality no longer compiles. A `readonly record struct` — which is what every key is supposed to be — satisfies it automatically.

The constraint is viral: it is repeated at all 13 declarations that carry a `TKey`. `EntityKeyConstraintTests` catches a new declaration that forgets it, which the compiler would only notice once such a type passes its `TKey` on to one of the bases.

## The state object

Every aggregate's state derives from the self-referencing `AggregateState<TSelf, TKey>`:

```csharp
public abstract record AggregateState<TSelf, TKey>
    where TSelf : AggregateState<TSelf, TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    public abstract TKey Id { get; init; }

    public long Version { get; init; }

    public abstract TSelf Apply(IDomainEvent domainEvent);

    internal TSelf WithVersion(long version) => (TSelf)(object)(this with { Version = version });
}
```

The `TSelf` type parameter lets `Apply` return the **concrete** state type rather than the base, so no casting is needed when evolving state.

It is a **record base, not an interface** ([ADR-0030](./decisions/0030-persisted-names-and-aggregate-version.md)): the record copy constructor is virtual, so `this with { … }` in the base returns the derived runtime type. That lets the base own the version bookkeeping outright — `WithVersion` is `internal`, so a state author can neither implement it wrongly nor reach it at all, and `AggregateRoot` needs no guard against a state that drops the version. The price is one unchecked cast, written once in Building Blocks instead of two mechanical lines in every state record.

The state object exists to keep large aggregates maintainable. All **apply/evolution logic lives on the state**, so the aggregate class contains only the public command API (the behavior invoked by callers).

State implementations are expected to be **immutable**: `Apply` returns the next state (`this with { … }`) rather than mutating in place.

```csharp
public sealed record RecipeState(RecipeId Id, string Name)
    : AggregateState<RecipeState, RecipeId>
{
    public static RecipeState Empty => new(default, string.Empty);

    public override RecipeState Apply(IDomainEvent e) => e switch
    {
        RecipeCreated created => this with { Id = created.RecipeId, Name = created.Name },
        RecipeRenamed renamed => this with { Name = renamed.NewName },
        _ => this
    };
}
```

The state writes nothing about the version: the base carries it, and `AggregateRoot` advances it on every folded event — including an event this `Apply` ignores.

### Child collections

A state may own children. Three rules apply, and two of them are enforced at runtime ([ADR-0031](./decisions/0031-aggregate-child-collections-as-owned-types.md)):

```csharp
public sealed record RecipeState(RecipeId Id, string Name)
    : AggregateState<RecipeState, RecipeId>
{
    public IReadOnlyCollection<Ingredient> Ingredients { get; init; } = new List<Ingredient>();

    public static RecipeState Empty => new(default, string.Empty);

    public override RecipeState Apply(IDomainEvent e) => e switch
    {
        IngredientAdded added => this with
        {
            Ingredients = Ingredients.Append(new Ingredient(added.IngredientId, added.Name)).ToList(),
        },
        IngredientRemoved removed => this with
        {
            Ingredients = Ingredients.Where(i => i.Id != removed.IngredientId).ToList(),
        },
        _ => this
    };
}
```

1. The collection is a `{ get; init; }` **property**, never a positional record parameter — EF Core would find "no suitable constructor".
2. It is built with `ToList()`, and never left `null`. A collection expression (`[]`, `[.. xs, x]`) assigned to `IReadOnlyCollection<T>` compiles to a read-only array, and EF Core adds and removes children through the collection instance itself; the runtime rejects a read-only, fixed-size or `null` value with a `NotSupportedException`.
3. The child is **owned** by the aggregate: it maps with `OwnsMany`, has its own strongly typed id declared as the key, and is reachable only through its parent. A navigation to an independent entity type, and an owned collection without a declared key, are both rejected at host startup.

The children may themselves own children — the commit reconciles the whole owned graph by key, at any depth. A single owned child (`OwnsOne`) works the same way; assigning `null` to it deletes it. An identity-less value collection may be mapped with `ToJson()` instead, in which case it is stored and replaced as one column.

A child is not a reference to another aggregate. To point at one, hold its typed id as a scalar ([ADR-0005](./decisions/0005-strongly-typed-aggregate-identifiers.md)).

### Child entities with behaviour

A child that has invariants of its own is modelled as an entity, not as a bare record ([ADR-0032](./decisions/0032-child-entities-raise-via-root.md)). Its data lives in an `EntityState<TSelf, TKey>` — the child counterpart of `AggregateState`, without a version, because the version belongs to the aggregate ([ADR-0030](./decisions/0030-persisted-names-and-aggregate-version.md)):

```csharp
public sealed record IngredientState(IngredientId Id, string Name, int Grams)
    : EntityState<IngredientState, IngredientId>
{
    public override IngredientState Apply(IDomainEvent e) => e switch
    {
        IngredientRegrammed regrammed => this with { Grams = regrammed.Grams },
        _ => this
    };
}
```

The behaviour sits on a thin hull over that state:

```csharp
public sealed class Ingredient : Entity<IngredientId, IngredientState>
{
    private readonly Recipe _recipe;

    internal Ingredient(Recipe recipe, IngredientId id)
        : base(recipe, id, recipe.FindIngredient) => _recipe = recipe;

    public int Grams => GetCurrentState().Grams;

    public void Regram(int grams)
    {
        RuleChecker.CheckValidationRule(new IngredientGramsMustBePositive(grams));

        RaiseEvent(new IngredientRegrammed(_recipe.Id, Id, grams));
    }
}
```

The root builds hulls on demand and keeps the lookup to itself:

```csharp
public IReadOnlyCollection<Ingredient> Ingredients =>
    State.Ingredients.Select(i => new Ingredient(this, i.Id)).ToList();

internal IngredientState? FindIngredient(IngredientId id) =>
    State.Ingredients.FirstOrDefault(i => i.Id == id);
```

Four properties follow, and they are the whole point of the design:

- **The root stays the sole owner of the uncommitted events.** `RaiseEvent` on the child hands the event to `AggregateRoot.RaiseEvent` through `IDomainEventRaiser`, which the root implements explicitly. One list, one order, one `ClearDomainEvents()`.
- **The child raising an event advances the aggregate version** — the same number that is the EF Core concurrency token and the projection watermark. A child-only change is therefore never invisible.
- **A hull is a view, not a copy.** `GetCurrentState()` looks the state up through the root on every call, so two hulls for the same child are equal and both see the latest data. It is a method, not a property, because it throws when the child was removed in the same command — reading a removed child must fail, not return stale values.
- **Nothing new happens on the fold path.** The child's state is part of the root's state, so `State.Apply(e)` folds it (usually by delegating to the child state's own `Apply`), and `LoadFromHistory` and `IStateOwner.Restore` rebuild children with no extra machinery.
- **Every entity has a state.** There is no state-less entity base: a child's data lives in the aggregate's state graph, a value without identity is a value object, and a child without behaviour needs no hull at all — only its record. `Entity<TKey, TState>` and `AggregateRoot<TKey, TState>` are the only two children of `EntityBase<TKey>`.

> A child entity never registers an event itself — it raises through the root.

## Aggregates and domain events

### One authoring model, two bases

Every aggregate derives (directly or indirectly) from the single base `AggregateRoot<TKey, TState>`:

```csharp
public abstract class AggregateRoot<TKey, TState>
    : EntityBase<TKey>, IAggregateRoot<TKey>, IDomainEventOwner, IDomainEventRaiser, IStateOwner
    where TKey : struct, IEntityKey, IEquatable<TKey>
    where TState : AggregateState<TState, TKey>
```

- It holds the current immutable `protected TState State` and the private uncommitted-events list.
- `Id` is derived from `State`.
- State changes **only** via `protected RaiseEvent(e)` — apply the event to the state, validate identity, and record the event. There is no other mutation path; the state fold forces every change to be expressed as an event.
- Events are exposed **read-only**; clearing is implemented **explicitly** via `IDomainEventOwner`.

**Event-sourced** aggregates extend `EventSourcedAggregateRoot<TKey, TState>`, which is purely **additive**:

```csharp
public abstract class EventSourcedAggregateRoot<TKey, TState>
    : AggregateRoot<TKey, TState>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
    where TState : AggregateState<TState, TKey>
```

- It adds an internal version (stream position) that advances on every raise/replay.
- It adds `LoadFromHistory(history)` — replay a persisted stream to rebuild state (rehydration; records nothing).
- It implements `IEventSourcedAggregateRoot<TKey>` (`LoadFromHistory`) **explicitly**, so replay is **not** on a concrete aggregate's public surface — a caller must deliberately cast to reach it. The aggregate's `Version` is **not** part of this interface: since [ADR-0030](./decisions/0030-persisted-names-and-aggregate-version.md) every aggregate carries one on its state, readable through `IStateOwner.Version`. Duplicating it here would have meant two names for one number.

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
IDomainEventOwner      → void ClearDomainEvents()                          (infrastructure only)

IDomainEventRaiser     → void Raise(IDomainEvent e)                        (child entities only)
```

- `IAggregateRoot<TKey>` inherits **only** `IHasDomainEvents`. Application code holding an aggregate therefore sees `DomainEvents` and **cannot** see `ClearDomainEvents()`.
- The aggregate base _also_ implements `IDomainEventOwner`, but **explicitly**:

```csharp
void IDomainEventOwner.ClearDomainEvents() => _domainEvents.Clear();
```

- Raising (`RaiseEvent`) is `protected`, so only the aggregate itself can add events. A child entity inside the aggregate reaches it through `IDomainEventRaiser`, which the root implements **explicitly** and hands to its own children only ([ADR-0032](./decisions/0032-child-entities-raise-via-root.md)).

The same explicit-implementation technique hides the event-sourcing capability on the ES base:

```text
IEventSourcedAggregateRoot<TKey>  → void LoadFromHistory(...)                  (ES infrastructure only)
IStateOwner                       → long Version; object State; Restore(...)   (persistence only)
```

Both `ClearDomainEvents` and the ES members are reachable **only** by code that deliberately casts to the respective interface — by convention, the persistence layer.

### The `*Owner` pattern, and reconstitution

`IDomainEventOwner` and `IStateOwner` are the same shape: the aggregate **owns** something (its events, its state), and infrastructure needs privileged access to it, so the contract is implemented **explicitly** and is invisible to domain code. The suffix is the signal — see one, expect the other three properties.

**Reconstitution** — the repository rebuilding a stored aggregate — needs an empty hull to fill via `IStateOwner.Restore` or `LoadFromHistory`. The hull comes from a **convention**, not a contract: every aggregate keeps a **private parameterless constructor**, and nothing else:

```csharp
public sealed class Widget : AggregateRoot<WidgetId, WidgetState>
{
    private Widget() : base(WidgetState.Empty) { }

    public static Widget Create(WidgetId id, string name) { … }
}
```

The private constructor keeps `new Widget()` a compile error everywhere, so the aggregate's named factory stays the only public way in. Infrastructure reaches the constructor through an internal, per-type-cached factory, and `AddBuildingBlocks` validates the convention **at host startup**: every aggregate in the assemblies named via `AddDomainEventsFrom` must have a parameterless constructor, or the host fails to start with a message naming the aggregate and the fix ([ADR-0025](./decisions/0025-unified-state-fold-aggregate-model.md) reconstitution amendment 2026-08-04). An earlier design expressed the same guarantee as an explicit `IReconstitutable<TSelf>` implementation on every aggregate; it was retired because the per-aggregate ceremony outweighed the compile-time proof — the amendment records the trade-off.

### Access matrix (event-sourced base)

| Caller holds…                             | Read events? | Clear events?              | Raise events?               | `LoadFromHistory`? |
| ----------------------------------------- | ------------ | -------------------------- | --------------------------- | ----------------------------------------- |
| The concrete aggregate (e.g. `Recipe`)    | ✅           | ❌ (not on surface)        | ❌ (only internally)        | ❌ (not on surface)                       |
| `IAggregateRoot<TKey>`                    | ✅           | ❌                         | ❌                          | ❌                                        |
| `IHasDomainEvents`                        | ✅           | ❌                         | ❌                          | ❌                                        |
| `IDomainEventOwner` (cast)                | ✅           | ✅                         | ❌                          | ❌                                        |
| `IEventSourcedAggregateRoot<TKey>` (cast) | ✅           | ❌                         | ❌                          | ✅                                        |
| A subclass of the ES base                 | ✅           | ❌ (explicit, not visible) | ✅ (`protected RaiseEvent`) | ❌ (explicit, not visible)                |

> The state-stored base is identical except that it has no `IEventSourcedAggregateRoot<TKey>` members at all (last column is not applicable). The aggregate's `Version` is missing from this matrix on purpose: it is not an event-sourcing member. Both bases carry it on their state, and both expose it the same way, through the explicitly implemented `IStateOwner` ([ADR-0030](./decisions/0030-persisted-names-and-aggregate-version.md)).

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
((IDomainEventOwner)agg).ClearDomainEvents()
```

## Domain events

`IDomainEvent` is a pure business marker:

```csharp
public interface IDomainEvent;
```

`DomainEvent` is an equally empty convenience base record. Events are **pure data** — plain value records with working value equality and **no identity fields**. Their `EventId` and `OccurredAt` are minted by the unit of work at commit time and travel on the `DomainEventEnvelope`, never on the event itself (ADR-0029).

> Domain events are **internal** to a service's domain. Translating them into integration events for cross-service messaging happens at the service boundary, not here.

An event's **type name** is declared with `[EventName]` and is therefore rename-proof. Its **field
names** are not: they are the CLR property names the serializer sees, so renaming a property renames
the stored field. Domain has no attribute for that on purpose — a field rename almost always changes
meaning and should cost a new event version. What makes it visible is a checked-in schema snapshot
per service, rendered by `PersistedSchema` in Infrastructure and asserted by a test (ADR-0035).

## Business rules and validation

Two distinct concepts, each with its own rule interface and exception:

| Concept                   | Rule interface          | Predicate     | Exception                        |
| ------------------------- | ----------------------- | ------------- | -------------------------------- |
| Invariant / business rule | `IBusinessRule`         | `IsBroken()`  | `BusinessRuleViolationException` |
| Input / domain validation | `IDomainValidationRule` | `IsInvalid()` | `DomainValidationException`      |

**Both** interfaces carry a stable, machine-readable `Code` — the identifier of the rule itself,
which is what lets a client react to *that* constraint instead of to "some business rule".
`IDomainValidationRule` additionally carries a `Target`, the name of the field the rule is about,
`null` for a rule spanning several fields. An invariant has no field, so `IBusinessRule`
deliberately has no `Target`; that argument is about the field and does not extend to the code.

`RuleChecker` evaluates them. The method name says which kind of rule is being checked, and
therefore which of the two exceptions can escape:

```csharp
RuleChecker.CheckValidationRule(new RecipeNameMustNotBeEmpty(name));
RuleChecker.CheckAllBusinessRules(rule1, rule2, rule3);
```

**The `CheckAll…` methods evaluate every rule and collect the broken ones**, throwing once at the
end. Both exceptions carry `IReadOnlyList<RuleViolation> Violations` — `RuleViolation(Code,
Target, Message)`, where `Code` is never `null` — so a caller gets every field error in one round
trip instead of one per request. The message-only constructors that CA1032 requires carry no rule
and therefore fill in the exception's own `FallbackCode` constant. `Exception.Message` is the
single message verbatim when there is one violation and the
messages joined by `"; "` otherwise; structured access goes through `Violations`, never by
parsing the message.

Consequence for rule authors: **rules in one `Check` call must be independent**, because a later
rule is evaluated even when an earlier one already failed. Where one rule only makes sense after
another has passed, write **two consecutive `Check` calls** — a pre-condition pass, then the
dependent pass. That staging is domain design, and it is what `Gadget` already does when a
validation rule precedes a business rule.

The two kinds are never mixed in one call: each overload takes one kind and throws its own
exception. Therefore a handler invocation raises at most one exception, and every `Failure` in
the resulting failed `Result` shares one category.

**A `null` rule is a bug, not a satisfied rule.** Every overload guards its argument with
`ArgumentNullException.ThrowIfNull`, and the `params` overloads guard the whole array **before**
the first rule runs — otherwise whether a `null` surfaced as an `ArgumentNullException` or
vanished behind collected domain errors would depend on its position in the argument list. The
earlier `rule?.IsBroken() == true` kept guard clauses short at the price of the validation layer
falling silent in exactly the case it exists for.

See [ADR-0009](./decisions/0009-business-rules-and-domain-validation.md).

## Usage example — an aggregate

The aggregate carries only the command API; the state carries the apply logic. The same code works for a state-stored aggregate (base `AggregateRoot<RecipeId, RecipeState>`) and an event-sourced one (base `EventSourcedAggregateRoot<RecipeId, RecipeState>`).

```csharp
public readonly record struct RecipeId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;
}

public sealed record RecipeState(RecipeId Id, string Name)
    : AggregateState<RecipeState, RecipeId>
{
    public static RecipeState Empty => new(default, string.Empty);

    public override RecipeState Apply(IDomainEvent e) => e switch
    {
        RecipeCreated created => this with { Id = created.RecipeId, Name = created.Name },
        RecipeRenamed renamed => this with { Name = renamed.NewName },
        _ => this
    };
}

public sealed class Recipe : EventSourcedAggregateRoot<RecipeId, RecipeState>
{
    private Recipe() : base(RecipeState.Empty) { }

    public static Recipe Create(RecipeId id, string name)
    {
        RuleChecker.CheckValidationRule(new RecipeNameMustNotBeEmpty(name));
        var recipe = new Recipe();
        recipe.RaiseEvent(new RecipeCreated(id, name));
        return recipe;
    }

    public void Rename(string newName)
    {
        RuleChecker.CheckValidationRule(new RecipeNameMustNotBeEmpty(newName));
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
3. All **apply/evolution logic lives on the state** (`AggregateState<TSelf, TKey>`), keeping aggregates free of apply noise.
4. **Identity validation is type-agnostic**, driven by `IEntityKey.IsEmpty`. Entities validate in the constructor; aggregates validate at **every transition** (no post-creation check).
5. Equality is **identity-based** and type-sensitive, implemented once on `EntityBase<TKey>`.
6. Aggregates **own** their events; outsiders read only; clearing is **explicit** and infrastructure-only.
7. **Event-sourcing capability** (`LoadFromHistory`, and nothing else) is exposed via **explicit** `IEventSourcedAggregateRoot<TKey>` implementation — off the public surface — and is guarded against replay misuse (throws if uncommitted events exist), which keeps snapshotting possible. The `Version` is **not** part of it: every aggregate carries one on its state, read through `IStateOwner` (ADR-0030).
8. Domain events are **pure value records** without identity fields; identity lives on the envelope (ADR-0029).
9. Business rules and domain validation are **distinct**, with distinct exceptions.

## Related documents

- [ADR-0006 — Aggregate owns its domain events](./decisions/0006-aggregate-owns-domain-events.md)
- [ADR-0007 — Read-only vs. managed domain events](./decisions/0007-read-only-vs-managed-domain-events.md)
- [ADR-0008 — Entity identity and equality](./decisions/0008-entity-identity-and-equality.md)
- [ADR-0009 — Business rules and domain validation](./decisions/0009-business-rules-and-domain-validation.md)
- [ADR-0010 — Aggregate state object](./decisions/0010-aggregate-state-object.md)
- [ADR-0025 — Unified state-fold aggregate model with additive event sourcing](./decisions/0025-unified-state-fold-aggregate-model.md)
- [ADR-0026 — Single repository contract](./decisions/0026-single-repository-contract.md)

