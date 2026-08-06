# 0034. A typed key serializes as its bare value

- **Status:** Accepted
- **Date:** 2026-08-06

## Context

ADR-0005 requires strongly typed identifiers, and every key type in this repository is a
`readonly record struct` implementing `IEntityKey<TValue>` with two members: the underlying
`Value` and a computed `IsEmpty` that the domain uses for its identity guards (ADR-0008,
ADR-0025).

`IsEmpty` is a domain predicate. It is derived from `Value` and carries no information of its
own. To a JSON serializer, however, it is an ordinary public property, so a typed key was written
as an object with two members:

```json
{ "GadgetId": { "Value": "8f3a…", "IsEmpty": false } }
```

That shape reached three stores, all of them append-only or contractual:

- Marten's `mt_events.data`, on the default serializer the store happened to pick.
- The domain-event outbox payload, serialized by `DomainEventEnvelopeSerializer` with
  `new JsonSerializerOptions(JsonSerializerDefaults.General)`.
- Integration-event bodies on RabbitMQ, serialized by Wolverine's default.

None of the three was a decision. All three were a default, and events are immutable: once a
production stream exists, changing the shape costs an event migration across every bounded
context. No production stream exists yet.

## Decision

A typed key serializes as its **bare underlying value**, in every JSON path Building Blocks owns:

```json
{ "GadgetId": "8f3a…" }
```

`EntityKeyJsonConverterFactory` in `BuildingBlocks.Infrastructure/Persistence/` produces an
`EntityKeyJsonConverter<TKey, TValue>` for any type implementing `IEntityKey<TValue>`. It writes
`Value` and reads the key back through the same single-argument constructor the EF Core value
converter already requires; both now share `EntityKeyActivator<TKey, TValue>`.

`EntityKeyJsonOptions` is the one place that adds the factory to a `JsonSerializerOptions`, and it
is applied at all three sites: the envelope serializer's options, Marten via
`UseSystemTextJsonForSerialization`, and Wolverine via `UseSystemTextJsonForSerialization` in
`BuildingBlocksWolverineExtension`.

Choosing System.Text.Json for Marten is part of the decision, not an incidental consequence. A
`[JsonIgnore]` attribute on `IEntityKey.IsEmpty` would have been the smaller change, but it binds
to one serializer, and Marten's default is the other one — the attribute would have been silently
ineffective exactly where the immutable data lives.

## Consequences

- The event stream, the outbox payload and the message body are readable by hand, and roughly half
  the size for a key-heavy event.
- `IsEmpty` stays a domain concept. It can be renamed, or its rule changed, without touching stored
  data.
- The format is now pinned by tests rather than inherited from a default:
  `EntityKeyJsonConverterTests` covers the converter, `DomainEventEnvelopeSerializerTests` the
  outbox payload, and `EntityKeyEventStreamFormatTests` asserts the raw `mt_events.data` column
  against a real PostgreSQL container.
- The read side is deliberately **not** tolerant of the old object shape. A single format is the
  point; there are no streams to be compatible with.
- Marten now serializes with System.Text.Json instead of its default. This affects Marten documents
  as well as events — the repository stores no Marten documents today, and the event store is the
  only Marten feature in use (ADR-0019).
- A key type without a public single-argument constructor fails when it is first deserialized,
  naming the key type. That constraint already existed for EF Core; it is now shared.
- Dictionary keys are out of scope. A typed key used as a JSON property name throws
  `NotSupportedException` from System.Text.Json rather than silently reverting to the object shape;
  no such dictionary exists in the repository.

## Alternatives considered

- **`[JsonIgnore]` on `IEntityKey.IsEmpty`.** One line, no converter — but it leaves the wrapping
  object, so the format still carries a member name that is a CLR detail, and it is a
  System.Text.Json attribute in `BuildingBlocks.Domain`, which would tie the domain to a serializer
  and still not reach Marten's default one.
- **Fix it in the sample key types.** The finding surfaced on `WidgetId`/`GadgetId`, but a rule
  every future key type must remember is not a rule. It belongs in Building Blocks.
- **Tolerate both shapes on read.** Cheap to write, but it makes the old shape permanent: nothing
  would ever remove the branch, and the format would stay two formats.
