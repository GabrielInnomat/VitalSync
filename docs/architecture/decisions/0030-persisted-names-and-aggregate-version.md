# 0030. Persisted names are declared, and the aggregate version is part of the state

- **Status:** Accepted
- **Date:** 2026-08-03

## Context

Three properties of the persistence format were left to accident, and all three become data
migrations the moment a real service writes a row.

**Event type names were CLR metadata.** Every outbox row stored
`Type.AssemblyQualifiedName` and was read back with `Type.GetType(..., throwOnError: true)`.
A version bump, an assembly rename or a namespace move made every not-yet-delivered message
unreadable — and the outbox is crash-recovery data, the one place where that is least
affordable. `Type.GetType` over persisted strings is also an unbounded type-activation surface.
The same problem existed one layer down and was worse: `UseMartenEventSourcing` configured no
event aliases at all, so Marten derived `mt_events.type` from the CLR type name. Renaming an
event class orphaned it in the event store itself, where there is no "just rebuild it".

**Stream keys were CLR metadata.** `EntityKeyFormatter` composed `$"{aggregateType.Name}/{id}"`.
Renaming `Gadget` to `Device` orphaned every existing stream with the most unpleasant failure
shape available: `FetchStreamAsync` returns empty, `GetByIdAsync` returns `null`, the handler
correctly reports `NotFound`. No error anywhere — just data that appears to have vanished, and
the next write opens a new stream that buries the old one for good.

**There was no aggregate version.** Only `EventSourcedAggregateRoot` counted events, in a
private field, for Marten's optimistic concurrency. State-stored contexts had no concurrency
control at all (no `RowVersion`, no `IsConcurrencyToken` anywhere), and `DomainEventEnvelope`
carried no aggregate metadata. ADR-0022 requires projection handlers to be "idempotent and
per-aggregate order-aware", but the infrastructure gave them nothing to be order-aware
*about* — both samples fell back to a business field (`RenameCount`) as a stand-in, which is
not a general technique.

## Decision

### Names are declared, never derived

Two attributes in `BuildingBlocks.Domain`, both validating lower-case kebab-case at
construction:

```csharp
[EventName("widget-created-v1")]
public sealed record WidgetCreated(WidgetId WidgetId, string Name) : DomainEvent;

[AggregateName("widget")]
public sealed class Widget : AggregateRoot<WidgetId, WidgetState>, IReconstitutable<Widget>;
```

`[AggregateName]` serves both the stream key prefix and the `AggregateName` field on the
envelope — one concept, one attribute, rather than a separate `[StreamPrefix]`.

A `DomainEventTypeRegistry` is built from the assemblies a host names via
`BuildingBlocksOptions.AddDomainEventsFrom(assembly)`. It maps name to type in both directions
and **throws at registration** for a missing `[EventName]` or a duplicate name. Configuring a
persistence strategy without registering any domain event assembly is itself an error, so
forgetting the call fails at startup rather than at the first commit.

The registry replaces `Type.GetType` in the serializer — the readable type set is now closed —
**and** feeds `options.Events.MapEventType` in the Marten wiring, so the event store and the
outbox agree on one name. Missing `[AggregateName]` throws in `EntityKeyFormatter`; the
contract must be set deliberately, not inherited from a refactoring accident.

### The version lives on the state, and the state becomes a record base

The `IState<TSelf, TKey>` **interface is replaced by the abstract record**
`AggregateState<TSelf, TKey>`, which carries `Id`, `Version` and `Apply`:

```csharp
public abstract record AggregateState<TSelf, TKey>
    where TSelf : AggregateState<TSelf, TKey>
    where TKey : struct, IEntityKey
{
    public abstract TKey Id { get; init; }

    public long Version { get; init; }

    public abstract TSelf Apply(IDomainEvent domainEvent);

    internal TSelf WithVersion(long version) => (TSelf)(object)(this with { Version = version });
}
```

A record's copy constructor is virtual, so `this with { … }` in the base returns the **derived**
runtime type. That is what an interface could not do: an interface has no body, so every state
record would have had to write `Version` and a mechanical `WithVersion` itself, and
`AggregateRoot` would have had to guard against a state that dropped the version. Here the base
owns the bookkeeping outright — `WithVersion` is `internal`, unreachable and unimplementable
from domain code, and the guard is unnecessary because the failure mode does not exist.

`AggregateRoot.ApplyEvent` therefore advances the version on every folded event, including an
event the state's `Apply` ignores. The private counter in `EventSourcedAggregateRoot` is gone —
both persistence styles now read the same number, exposed through the explicit,
infrastructure-only `IStateOwner.Version` and `IEventSourcedAggregateRoot.Version`.

Because ADR-0025 persists the state and not the aggregate, the version is an ordinary mapped
column, and a state-stored context maps it as `IsConcurrencyToken()`. `EfCoreUnitOfWork`
already copies current values onto the tracked entry, so EF compares the *original* version in
the `WHERE` clause and a lost update becomes `DbUpdateConcurrencyException` — which
`UnitOfWorkBehavior` already translates to `FailureCategory.Conflict`.

The envelope carries the aggregate metadata, and `DomainEventMetadata` mirrors it:

```csharp
public sealed record DomainEventEnvelope(
    string EventName, string Payload, Guid EventId,
    string AggregateName, string AggregateId, long Version, DateTimeOffset OccurredAt);
```

Each event in a commit gets its own version, counted back from the aggregate's final version,
so a projection sees a strictly increasing per-aggregate sequence.

### Projection handlers receive the metadata

`IProjectionHandler<TDomainEvent>.Handle` gains a `DomainEventMetadata` parameter. Without it
the version reaches the envelope and stops there, and ADR-0022's order-awareness requirement
stays unimplementable. A projection keeps the last processed version on its read model and
ignores anything at or below that watermark.

## Consequences

- Renaming an event or aggregate class is now a refactoring, not a data loss. Versioning events
  becomes expressible at all: `-v2` next to `-v1`.
- A domain event without `[EventName]` and an aggregate without `[AggregateName]` fail loudly —
  at registration and at first use respectively, never silently at runtime.
- State-stored contexts have optimistic concurrency for the first time.
- **State records gain no boilerplate at all** — they declare their fields, `Empty` and
  `override Apply`, and say nothing about the version. The cost is one unchecked cast, written
  once in Building Blocks.
- A state record can no longer have a different base class, and its `Id` is declared
  `{ get; init; }`. Neither is a practical loss: single inheritance is what a state wants
  anyway, and a positional record already generated an `init` setter for `Id`.
- Amends [ADR-0010](./0010-aggregate-state-object.md): the state object is described by an
  abstract record rather than an interface. `Apply` still returns the concrete state, so the
  self-referencing shape that ADR-0010's amendment introduced is unchanged.
- **Breaking change for projection handlers** — every `Handle` gains a parameter. Four handlers
  in the samples; none in production code yet, which is why this lands now.
- The watermark rule changes projection semantics: an event at or below the watermark is
  **ignored**, where the previous per-field merge would still have applied disjoint fields.
  That is correct under the current transport, which delivers per-aggregate in order
  (ADR-0022, sequential local queue) and only ever *redelivers*; it would not be correct under
  genuinely unordered delivery.
- TODO-20 (partitioning the domain event queue by aggregate instead of serialising it globally)
  becomes possible — the envelope now carries the aggregate identity it needs.
- Existing sample data is orphaned (streams move from `Gadget/…` to `gadget/…`). Deliberate and
  free today; the whole point of doing this before the first real service.

## Alternatives considered

- **Keep `AssemblyQualifiedName`, pin assembly versions.** Cheapest, but it makes the assembly
  identity part of the persistence contract and leaves `Type.GetType` on stored data.
- **Version as an EF shadow property, aggregate-owned.** No boilerplate in state records, but
  the number becomes invisible in the model and adds a second infrastructure back door next to
  `IStateOwner`, for a value that is genuinely part of the persisted state.
- **Keeping `IState` as an interface**, with `Version` and a hand-written `WithVersion` on every
  state record plus a runtime guard in `AggregateRoot`. This was built first and then replaced:
  it costs two mechanical lines per state, keeps a failure mode alive (a state that drops the
  version) and needs a guard, an exception and a test to cover it. Its one advantage is that it
  contains no unchecked cast.
- **A source generator** emitting the interface version's boilerplate. Purely additive, but four
  lines across an estimated fifteen aggregates do not justify a generator project, its tests,
  build integration and the debugging of generated code — and it would leave the guard in place.
- **Leave projections on `Handle(TEvent, ct)`.** Smaller change, but the version would reach the
  envelope and die there, leaving ADR-0022's requirement as unbacked prose.
