# 0035. A persisted field name is pinned by a snapshot, not by an attribute

- **Status:** Accepted
- **Date:** 2026-08-06

## Context

ADR-0030 removed derived names at the **type** level: a domain event declares `[EventName]`, an
aggregate declares `[AggregateName]`, and a CLR rename no longer touches stored data. It left the
**field** level open.

An event body is JSON, and without an explicit name the JSON name is the CLR property name. Renaming
`Titel` to `Name` therefore renames the field on the wire. Stored events keep the old name, the
deserializer finds nothing for the new one and leaves the property at its default. There is no
exception, no log entry and no failing test — the same silent-corruption class as ADR-0030's
`Type.GetType` over stored data and the stream key derived from the class name, both of which this
repository treated as blocking.

The state-stored path does not have this problem. Every property of an `AggregateState` is mapped
with `HasColumnName`, and a relational schema can contradict: a column that no longer exists is an
error, not a default. The EF migration history is that path's snapshot. The gaps are the paths where
data is stored as **JSON without a schema**: event bodies, integration-event bodies, and children
mapped with `ToJson()`.

The event-sourced aggregate state is not persisted anywhere today — it only ever exists in memory,
rebuilt by `LoadFromHistory` — so it has nothing to protect. That changes the moment Marten
snapshotting is enabled, which ADR-0019 defers as "additive". Additive is true for the events; it is
not true for the state, which a snapshot promotes from "stored nowhere" to "stored as JSON without a
schema" as a side effect of a performance decision.

## Decision

**Rename tolerance is bought with attributes; rename visibility is bought with a snapshot. Field
names get the second.**

A `[JsonPropertyName]` per field would make a rename free. That is the wrong goal here. A field
rename is almost always a change of meaning — `IngredientId` becoming `WorkoutId` is a different
field, and an attribute mapping the new property onto the old stored name would be a permanent lie.
Attributes stay where a rename is frequent *and* meaning-preserving, which is the type level:
`Created`/`Registered`/`Added` are the same event. They also do not scale: roughly one attribute per
type is bearable, five per type is not.

So a rename that breaks data is allowed to hurt. It is not allowed to be silent.

**1. The persisted event schema is snapshotted.** `PersistedSchema` in
`BuildingBlocks.Infrastructure/Schema/` renders every domain event and every integration event of a
set of assemblies into a deterministic text file and compares it against an approved baseline
checked in next to the test:

```text
domain-event widget-created-v1
  Name : string
  WidgetId : guid

integration-event sample-state-stored.widget-created
  EventId : guid
  Name : string
  OccurredAt : datetimeoffset
  WidgetId : guid
```

Three properties of the rendering matter:

- It reads through **`JsonTypeInfo`**, not `Type.GetProperties()`. It therefore pins what the
  serializer actually does, including a `[JsonPropertyName]` and a future `PropertyNamingPolicy`.
- A typed key renders as the **value it serializes to** (`guid`, `int`), so ADR-0034 is visible in
  the baseline instead of hidden behind a key type name.
- Blocks and fields are **sorted by name**. Reordering members is meaningless for JSON and must not
  produce a diff.

The baseline lives with the service that owns the events, not in Building Blocks, and this is a
**test** rather than a start-up check: a snapshot needs a checked-in baseline, which does not exist
at run time.

**2. A stored field name is declared for the state-stored path too.** `AggregateStateModelCheck`
now rejects at start-up any property of an `AggregateState` or of one of its owned children that has
no explicit `HasColumnName` — or, for a `ToJson()` child, no explicit `HasJsonPropertyName`. The
discipline was already followed everywhere; it is now enforced, for the same reason ADR-0033 made
`ApplyEntityKeyConversions` fail loudly rather than compensate.

**3. Enabling Marten snapshotting means putting the aggregate state into the baseline.** The
selection criterion of the renderer is *everything persisted as JSON*, not *every domain event*, and
a snapshotted state is persisted as JSON.

## Consequences

- Adding a field is a one-line baseline update. Renaming, removing or retyping one turns the build
  red with the rule attached to the failure: a field that was only added stays readable, so approve
  the snapshot; a field that was renamed, removed or retyped does not, so leave the event alone and
  introduce a successor under a new `[EventName]`.
- The check runs against the **contract**, not against a database, so it needs no container and
  costs milliseconds.
- A failing run writes its rendering to `*.received.txt` next to the baseline (git-ignored), so
  approving a change is a file copy.
- `PersistedSchema` is public API of `BuildingBlocks.Infrastructure` even though only tests call it.
  `PublicSurfaceTests` lists it separately under `IntendedTestingApi` so that the exception stays
  visible.
- The snapshot pins **names**, not wire formats. A leaf type whose converter changes shape — an enum
  gaining `JsonStringEnumConverter`, say — is not detected. Names are where the silent failures were.
- Read models are deliberately **out of scope**: a read model is derived and rebuildable, so a field
  rename there costs a rebuild, not data. This holds only as long as rebuilding is actually possible,
  which is the open point tracked as TODO-21 for state-stored contexts.

## Alternatives considered

- **`[JsonPropertyName]` on every field.** Roughly 200 attributes per bounded context, and most of
  them would eventually be lying, because a field rename usually means the field changed. Rejected
  on cardinality and on honesty.
- **Deriving the JSON name from a naming policy.** Stabilises casing but not identity: `Titel` and
  `Name` produce different keys under any policy. It solves the wrong half.
- **A start-up check instead of a test.** There is nothing at run time to compare against; the
  baseline is a repository artefact.
- **Dropping `[AggregateName]` for symmetry.** It stays, but its justification is corrected: not
  rename tolerance, which nobody uses, but that the stream key is a validated kebab-case *format*
  where a derived conversion has edge cases, and that it carries the highest migration cost of any
  name in the system — it appears in every stream key and on every envelope.

## Amendment (2026-08-07) — the read-model exclusion is now genuinely covered

Read models were excluded from the snapshot as "derived and rebuildable". On the event-sourced path
that was true from the start; on the state-stored path it was an assumption, because nothing could
re-derive a read model after the outbox row was gone. ADR-0036 supplies that missing tool, so the
exclusion now rests on a mechanism rather than on an intention.