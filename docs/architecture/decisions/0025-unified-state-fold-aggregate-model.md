# 0025. Unified state-fold aggregate model with additive event sourcing

- **Status:** Accepted
- **Date:** 2026-07-30
- **Supersedes:** [ADR-0012](./0012-optional-event-sourcing-aggregate.md)
- **Amended:** 2026-08-02 (EF Core maps the state as an entity type, not the aggregate — see the note below)
- **Amended:** 2026-08-03 (reconstitution is a domain contract, not a constructor requirement — see the note below)

## Context

[ADR-0012](./0012-optional-event-sourcing-aggregate.md) split the aggregate hierarchy into a state-modeled
`AggregateRoot<TKey>` (free properties, `AddDomainEvent`) and an event-modeled
`EventSourcedAggregateRoot<TKey, TState>` (state fold via `RaiseEvent`, `Version`, `LoadFromHistory`). Apart from
`IAggregateRoot<TKey>` and the equality logic the two bases shared **nothing**: identity acquisition, state
representation, the event-raising API, versioning, construction, and the repository contract all differed.

That made the declared strategy — "event sourcing selectively, where it carries business value" — structurally
unsupported. The value of event sourcing typically becomes apparent only after a business need for history emerges,
so a framework backing that strategy must make the *later* switch cheap. Under the split hierarchy the switch was a
re-implementation: new base class, new state type, rewritten methods, a different repository interface, different
tests. Two programming models also meant double onboarding, double conventions, and double review rules
(IMP-10 in `Improvements.md`).

A further irritation: `Entity<TKey>`, `AggregateRoot<TKey>`, and `EventSourcedAggregateRoot<TKey, TState>` carried
three identical copies of the identity-equality implementation.

## Decision

Provide a **single authoring model** in which the persistence strategy is a configuration and repository decision,
not a class-hierarchy decision. The state fold is used by **all** aggregates:

- **`AggregateRoot<TKey, TState>`** — the one base for every aggregate. It holds the immutable
  `protected TState State` ([ADR-0010](./0010-aggregate-state-object.md)), derives `Id` from `State.Id`, and exposes
  a single mutation path: `RaiseEvent(IDomainEvent)` folds the event into the state
  (`State = State.Apply(event)`), validates the identity, and records the event for dispatch. The state fold is
  valuable even without event sourcing: it forces every state change to be expressed as an event instead of slipping
  past via a property setter — which is exactly why aggregates produce events in the first place.
- **`EventSourcedAggregateRoot<TKey, TState>`** — derives from `AggregateRoot<TKey, TState>` and adds **only** the
  event-sourcing capability: a `Version` for optimistic concurrency and `LoadFromHistory` for replay, both still
  explicitly implemented behind the `IEventSourcedAggregateRoot<TKey>` view ([ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)
  amendment, [ADR-0019](./0019-event-store-technology-marten.md)).
- **`EntityBase<TKey>`** — a new common base holding the single identity-equality implementation
  ([ADR-0008](./0008-entity-identity-and-equality.md)); `Entity<TKey>` (eager, constructor-validated identity) and
  `AggregateRoot<TKey, TState>` (state-derived, per-transition-validated identity) both derive from it. Its
  constructor is `private protected`, so domain code derives from those two, never from it directly.

Switching an aggregate between the state-stored and event-sourced worlds is now **additive**: change the base class
(the business code, state type, and tests stay untouched) and change the repository registration in the composition
layer (see [ADR-0026](./0026-single-repository-contract.md), which merges the repository contracts so even the
handler code is unaffected).

Differences from the *first* unification attempt ([ADR-0011](./0011-unified-aggregate-for-es-and-ef.md)): the
event-sourcing members (`Version`, `LoadFromHistory`) are **not** present on every aggregate — they live only on the
derived event-sourced base — so a state-stored aggregate carries no replay surface at all, while the authoring model
(state fold) is still shared.

**Trade-off, accepted knowingly:** for simple CRUD-like aggregates the fold is ceremony, and EF Core must map the
`State` record (via `ComplexProperty`/owned-entity or explicit column mapping) instead of plain auto-properties.
This price is paid for a consistent single model and a cheap ES migration path; the previous state — an advertised
strategy without structural support — was judged worse.
_The named mapping mechanism is superseded by the state-mapping amendment below; the trade-off itself stands._

> **State mapping (amendment 2026-08-02).** The trade-off above is right that EF Core must map the state and wrong
> about how. The first real EF Core consumer of the Building Blocks (the walking skeleton under `samples/`) showed
> that the aggregate cannot be mapped **at all**, and that the mechanisms named above are not the way out.
>
> - **The aggregate is not mappable.** `public sealed override TKey Id => State.Id` is computed: no setter, no backing
>   field, so EF Core refuses it as a primary key — *"No backing field could be found for property 'Widget.Id' and the
>   property does not have a setter."* A setter cannot be retrofitted in the override either, because
>   `EntityBase<TKey>.Id` is `public abstract TKey Id { get; }` (`CS0546`). Every possible fix therefore either
>   touches the domain or changes the persistence approach.
> - **`ComplexProperty`/owned-entity is not the way out.** Mapped as a complex type, a positional record state fails
>   with *"No suitable constructor was found"*, because EF must bind every constructor parameter and `State.Id` can
>   therefore not be ignored. This follow-on error **masks** the one above; the order in which EF reports the two is
>   misleading.
> - **What is mapped is the state, as an ordinary entity type.** The aggregate is behavior; the state is an immutable
>   record carrying identity ([ADR-0010](./0010-aggregate-state-object.md)) — a persistence object already. As an
>   *entity type* rather than a complex type both problems disappear: on a positional record `State.Id` is an
>   auto-property with a backing field, and `ApplyEntityKeyConversions` iterates `Model.GetEntityTypes()` and
>   therefore reaches it. The schema result is **one table with one identity column** — no shadow key, no duplicated
>   id, and a plain `modelBuilder.Entity<TState>(…)` block per aggregate.
>
> Mechanically this rests on one domain addition, deliberately kept out of the authoring model:
>
> - **`IStateOwner`** (`BuildingBlocks.Domain`) — `StateType`, `State`, `Restore(object)` — implemented
>   **explicitly** by `AggregateRoot<TKey, TState>`, exactly like `IDomainEventsManager`. Domain code never sees the
>   members, so an aggregate cannot bypass the event fold by restoring a state by hand, and only
>   `BuildingBlocks.Infrastructure` consumes it. `Restore` rejects a state with an empty identity and raises no domain
>   event: resuming from persisted state is not a state change in the domain sense.
>
> How the repository and the unit of work use it — including the rehydration constructor a state-stored aggregate now
> needs — is recorded in the tracking amendment of [ADR-0026](./0026-single-repository-contract.md).
> _The rehydration constructor named there is superseded by the reconstitution amendment below._
>
> **This ADR's decision is unchanged, and so are its neighbours.** `Id => State.Id` stands verbatim;
> [ADR-0008](./0008-entity-identity-and-equality.md), [ADR-0010](./0010-aggregate-state-object.md) and
> [ADR-0026](./0026-single-repository-contract.md) need no change to their decisions. The alternatives that *would*
> have touched them were considered and rejected: a shadow key filled by the repository (a permanent redundant id
> column per aggregate table), a `private protected` setter on `EntityBase.Id` (identity would have two sources, and
> ADR-0008/0010/0025 would all need amending), and a separate persistence object with translation (breaks ADR-0026's
> single repository contract). Consequence "EF Core mapping targets the state record" below is thereby true not only
> in effect but structurally: the state **is** the mapped entity type. Pinned by `EfCoreAggregateRoundTripTests`
> (Testcontainers, real PostgreSQL: create → rename → reload in a fresh scope).

> **Reconstitution (amendment 2026-08-03).** The state-mapping amendment above left the two persistence paths
> disagreeing about how the empty aggregate hull comes into being: `EfCoreRepository` used
> `Activator.CreateInstance(…, nonPublic: true)`, `MartenEventSourcedRepository` a `new()` constraint. Both are
> answers to a badly posed question — "public constructor or reflection?" — when what the repository actually needs
> (the state type, and an instance to fold into) is known at **compile time**. The `new()` variant additionally
> demanded a **public** parameterless constructor, which would have made `new Widget()` legal everywhere and let an
> unidentified aggregate reach domain code.
>
> Neither is kept. Reconstitution becomes an explicit domain contract:
>
> - **`IReconstitutable<TSelf>`** (`BuildingBlocks.Domain`) — `static abstract TSelf CreateEmpty()`. Reconstitution
>   is not creation: the repository does not author a new aggregate, it rebuilds an existing one, by restoring its
>   persisted state (`IStateOwner.Restore`) or replaying its history (`LoadFromHistory`). Both need an instance
>   first, and this supplies it.
> - **Implemented explicitly, constructor private.** A static abstract member is callable *only* through a type
>   parameter constrained to the interface, so an explicit implementation leaves no publicly reachable empty
>   constructor. Verified against the compiler: `new Widget()` is `CS1729`, `Widget.CreateEmpty()` is `CS0117`, and
>   reaching it through an interface-typed instance is `CS0176`. The aggregate's own named factory stays the only
>   public way in — the same technique already used for `IStateOwner` and `IDomainEventOwner`, lifted to a static
>   member.
> - **The constraint sits on `IRepository<TAggregate, TKey>`, not on the implementations.** An aggregate that cannot
>   be reconstituted therefore fails to compile where the repository is *injected*, instead of throwing when the
>   container first closes the open generic. That is the compile-time expressiveness the earlier note recorded as
>   unavailable; the startup scan it proposed as a substitute is not needed.
> - **Both paths are now identical.** The EF Core/Marten asymmetry was never a decision, only an accident of who was
>   written first.
>
> **The decision is unchanged.** The authoring model, `RaiseEvent` as the sole mutation path, and `Id => State.Id`
> all stand. What changes is a mechanism: "a state-stored aggregate needs a parameterless constructor (may be
> non-public)" becomes "**every** aggregate implements `IReconstitutable` explicitly and keeps its parameterless
> constructor private". Residual hole, accepted knowingly: application code *could* declare its own generic method
> constrained to `IReconstitutable` and obtain a hull that way. That is a loud, greppable, deliberate act rather than
> a typo, and reflection was never preventable anyway — the bar is "cannot happen by accident", not "cannot happen".
> Pinned by `ReconstitutableTests` (both persistence shapes) and by an `AggregateConventionTests` scan in each sample
> that fails on an aggregate missing the interface or exposing a public parameterless constructor.

## Consequences

- One programming model to learn, review, and tool; the "which base class?" question is now purely "does the event
  history carry business value?".
- The state-stored → event-sourced switch is a base-class change plus a composition-layer change; domain logic,
  state types, handlers, and tests survive unchanged.
- The timestamp/stamping path and the raising API are identical for all aggregates; divergence bugs between the two
  models can no longer occur.
- Identity equality is implemented exactly once (`EntityBase<TKey>`).
- State-stored aggregates gain the immutable state fold: better testability, no mutation outside events.
- EF Core mapping targets the state record, which costs more configuration than mapping free properties.
- `AddDomainEvent` no longer exists; `RaiseEvent` is the single raising API.
- Every aggregate carries one line of reconstitution boilerplate (a private constructor plus an explicit
  `CreateEmpty`). It cannot be inherited from the base, which would need a `TSelf` type parameter and therefore a
  `new()` constraint again — the very thing being removed. A source generator could emit it later; that is additive.

## Alternatives considered

- **Keep the split hierarchy (ADR-0012):** rejected — it makes the selective-ES strategy fictional, because the
  switch is maximally expensive precisely when its need is discovered.
- **Declare the ES↔state-stored switch an unsupported scenario:** honest, but rejected — history-driven requirements
  are expected in the health/fitness domain, so the migration path carries real value.
- **Return to the single base with ES members on every aggregate (ADR-0011):** rejected — it leaks
  `Version`/`LoadFromHistory` onto aggregates that never replay; the derived-class split keeps ES strictly additive.
