# 0031. Aggregate child collections map as owned types

- **Status:** Accepted
- **Date:** 2026-08-04

## Context

ADR-0025 maps the aggregate's **state**, not the aggregate, and ADR-0026 lets the repository track
that state while the EF Core unit of work copies the current state onto the tracked entry with
`CurrentValues.SetValues` before saving. `CurrentValues` covers **scalar properties only**. A state
that owns a child collection therefore behaved as follows:

- `EfCoreRepository.GetByIdAsync` called `FindAsync(stateType, [id])`.
- `EfCoreUnitOfWork.CommitAsync` copied the scalars of the new state onto the tracked instance.
- The children of the new state were never looked at. Added children were not inserted, changed
  children were not updated, removed children were not deleted.

Nothing failed. `SaveChangesAsync` reported success, the outbox received the domain events, the
projections updated the read model — and the write database silently disagreed with all of it. The
aggregate is the consistency boundary of this architecture, so an aggregate that cannot hold a
child collection is not a usable aggregate, and one that loses it quietly is worse than one that
refuses to.

Two shapes were measured against EF Core 10 before deciding (`EfCoreChildCollectionTests`):

1. Children as **owned types** (`OwnsOne`/`OwnsMany`, optionally `ToJson`).
2. Children as **independent entity types** reached through ordinary navigations, reconciled by a
   generic graph merger in Building Blocks.

The measurements settled two questions that had been argued from intuition:

- EF Core loads owned dependents **with their owner**, including through the non-generic
  `context.FindAsync(type, key)` the repository uses. No `Include`, no recursive `LoadAsync`, no
  `AutoInclude` convention is needed. Independent entity types are loaded by none of these.
- When the whole collection is replaced by **new instances carrying the same key values**, EF Core
  matches owned dependents **by key**, not by reference: kept children produce `UPDATE`, removed
  ones `DELETE`, new ones `INSERT`. Row identity is stable, so the feared delete-and-reinsert churn
  does not exist. That match is **one level deep**, though — see the amendment below.

## Decision

**A child of an aggregate maps as an owned type with an explicit domain key.**

- The child collection is a property of the aggregate **state**, typed as a writable
  `IReadOnlyCollection<T>` and rebuilt in `Apply` like every other part of the state.
- The write model maps it with `OwnsMany(...)`, gives it its own table, and declares the child's
  own strongly typed id as the key (`HasKey`). `ToJson()` stays available for children that are
  genuinely identity-less values.
- `EfCoreUnitOfWork` hands the current state to `AggregateStateGraph.Reconcile`, which copies the
  scalars **and** reconciles the whole owned graph against the tracked one **by key**, at any depth.
- `EfCoreRepository` keeps `FindAsync`; owned children arrive with their owner.

**The graph is reconciled by key, not by instance.** Assigning a replacement collection to a
navigation of the tracked entry — the obvious one-liner — only works one level deep: EF Core matches
the *directly* assigned dependents by key, but tracks the children *those* carry as new instances
and throws `The instance of entity type 'X' cannot be tracked because another instance with the same
key value is already being tracked`. So `Reconcile` walks the owned graph itself: a child that
still exists has its scalars copied onto the tracked child (`CurrentValues.SetValues`) and is then
recursed into, a new child is added to the tracked collection, a vanished one is removed from it.
EF Core turns that into `UPDATE` / `INSERT` / `DELETE` with stable row identity. The walk is driven
by EF Core metadata — `GetGetter()`, `FindPrimaryKey()`, `GetCollectionAccessor()` — not by
reflection over the CLR type, and it has no notion of composite keys, shadow foreign keys or cycles
because the conventions below make those unreachable.

A collection mapped with `ToJson()` is the one exception: it is a single column, its dependents
carry a synthesized shadow key, and replacing the instance is exactly right. `Reconcile`
detects that case by the absence of a single non-shadow key property and assigns.

**Three conventions are enforced, loudly.**

- A navigation from an `AggregateState` to an **independent** entity type is rejected at host
  startup by `AggregateStateModelStartupValidator`, naming the state and the navigation. A model
  that would lose data does not get to run.
- An owned **collection** that is not mapped to JSON must declare a **single, non-shadow key**
  (`HasKey`) — the child's own strongly typed id. Without it the commit has nothing to match a
  replaced child against and would rewrite rows instead of updating them. Also rejected at startup.
- A child collection whose runtime value is **read-only, fixed-size or null** is rejected on the
  spot with a `NotSupportedException` naming the collection and its runtime type — at any depth of
  the owned graph, not just on the state itself. This is not hypothetical: a collection expression
  assigned to `IReadOnlyCollection<T>` compiles to `Array.Empty<T>()` or to a compiler-generated
  read-only array, never to a `List<T>`, and EF Core adds and removes dependents through the
  collection instance itself. `null` is rejected rather than read as "no children", because the two
  are indistinguishable at the point where the difference costs rows.

**Two authoring rules follow from the C# side and are documented, not enforced:**

- The collection is a `{ get; init; }` property, **not** a positional record parameter — a
  positional collection parameter makes the state unconstructible by EF Core ("no suitable
  constructor was found").
- Build the collection with `ToList()` / `new List<T>()`. Never with a collection expression.

## Consequences

- Child collections round-trip: created, loaded, updated and deleted, with stable row identity and
  the parent's `Version` still acting as the optimistic concurrency token (ADR-0030). A child-only
  change advances that version, because it went through `RaiseEvent` like everything else.
- The aggregate boundary is now enforced by the mapping itself. An owned child has no `DbSet`, no
  independent lifetime and no cross-aggregate reference — exactly the DDD rule, expressed in the
  model instead of in a review comment.
- Referencing another aggregate from a state is impossible by construction; do it by holding that
  aggregate's typed id as a scalar, which is what ADR-0005 intends anyway.
- Children are not independently queryable in the write store. That is not a loss: queries belong
  to the read models (ADR-0022).
- `ApplyEntityKeyConversions` had to move from the builder API to the metadata API, because owned
  types are shared-type entity types and `modelBuilder.Entity(clrType)` throws for them. Typed
  child keys work as a result.
- Building Blocks stays small. The rejected option 2 would have added a reflective graph diff
  (composite keys, shadow foreign keys, cycles, reference-null transitions, orphan removal) —
  the very class of bug this ADR closes.
- Adding option 2 later remains possible and is not a data migration, because the startup validator
  guarantees no model with free navigations exists in the meantime.

## Alternatives considered

- **Generic graph merger over arbitrary navigations.** Maximum modelling freedom, but 150-250
  lines of reflective diffing in the one place where a bug is silent, plus a recursive load path
  the repository would have to own. It also invites treating children as independent entities,
  which dissolves the aggregate boundary. The merge that was in fact built is a much smaller thing:
  it walks **owned** graphs only, is driven by EF Core metadata rather than reflection, and is
  spared composite keys, shadow foreign keys, cycles and orphan-removal rules because the enforced
  conventions make all of them unreachable.
- **`ToJson()` as the default for every child collection.** Rejected as a default: a child with its
  own identity deserves a row and a key, and the key stability that made option 1 attractive is a
  relational property. `ToJson` remains the right answer for identity-less value collections.
- **Documenting the limitation and forbidding child collections.** Rejected: an aggregate is a
  consistency boundary over a graph. Forbidding the graph forbids the pattern.
- **Detecting the loss at commit and throwing.** This was the first step taken (fail loudly instead
  of silently) but it is not a destination — it declares the framework unable to do the one thing
  the domain model asks of it.

## Amendment, 2026-08-04: depth, single children and JSON

The original decision was measured on a **flat** child collection and on that evidence claimed a
hand-written graph diff buys nothing. Widening the tests to a two-level owned graph
(`Cart` → `CartLine` → `CartTag`) disproved the second half of that claim: assigning a replacement
collection to the tracked entry's navigation matches the *directly* assigned dependents by key, but
the grandchildren those carry are new instances, and EF Core refuses to track them next to the ones
it already holds under the same key. The commit threw instead of losing data — the guard rails held
— but a two-level aggregate simply did not work.

`AggregateStateGraph.Reconcile` therefore reconciles by key **itself**, at every depth, as
described under Decision. Three further shapes are now covered by tests rather than by assumption:

- **`OwnsOne`** (a single owned child): reconciled in place when both sides are present, assigned
  when either side is `null`, so clearing it deletes the row instead of orphaning it.
- **`ToJson`** collections: kept on the assignment path, since a JSON column has no per-child row to
  match and its dependents carry a synthesized shadow key.
- **`null` child collections**: rejected with the same `NotSupportedException` as read-only ones.

The declared-key convention is the price of the by-key walk and is now enforced at startup. It was
implicit in the original decision ("declares the child's own strongly typed id as the key"); it is
now checked.

> **Amendment (2026-08-05): the metadata-API move was only half the story.**
>
> The consequence above notes that `ApplyEntityKeyConversions` had to move from the builder API to
> the metadata API because owned types are shared-type entity types. That move preserved the
> helper's silent side effect: `AddProperty` mapped **any** CLR property of key type, including one
> the model had explicitly ignored and one that was computed and get-only. The first produced a
> column nobody asked for; the second broke model creation with "No backing field could be found".
>
> **ADR-0033 removes the cause rather than the symptoms:** the helper no longer discovers or adds
> properties at all, so a typed key is mapped explicitly — which every context here, including the
> `OwnsMany` configuration this ADR mandates, already does. Owned types are unaffected either way:
> they are separate entity types, so their properties are configured through `OwnsMany` and found
> by `FindProperty`.
