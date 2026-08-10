# 0036. A state-stored read model is rebuilt from the current state, not from a replay

- **Status:** Accepted
- **Date:** 2026-08-07

## Context

ADR-0022 makes read models "derived and rebuildable by replaying events / re-running projections".
That promise holds in an event-sourced context, where Marten keeps the stream. It does **not** hold
in a state-stored context: the outbox row is deleted once Wolverine has delivered it, so after the
flush the only surviving record of a domain event is the effect it had on the read model. A
projection with a bug therefore produces a read model that cannot be repaired, and a new read-model
field can only ever be filled for aggregates that change again afterwards.

Concretely: `WidgetPartAddedProjection` increments `PartCount`. Ship it with `PartCount--`, notice a
week later, fix the handler — every row written in that week stays wrong forever, because nothing
can re-deliver the events that produced them.

Three routes were considered.

**A domain-event journal.** Write every `DomainEventEnvelope` into an append-only table in the write
database and replay from there. This rebuilds event sourcing inside the state-stored path without
taking its benefits: storage and a retention policy become permanent obligations, and a replay puts
events back on the wire, which forces cross-context deduplication (TODO-14 part B) into scope for a
purely local repair.

**Deriving the live path from state.** Let the projection runner reload the aggregate and hand
handlers the current state instead of the event. One derivation path, no divergence — but it
devalues the domain event for projections, moves read load onto the write database, and would have
touched eleven files in Building Blocks.

**Rebuilding from the current state as a second, explicitly invoked path.** The write database
already holds the authoritative state of every aggregate. A read model is a function of that state,
so it can be derived from it directly without any history.

The standing objection to the third route is that two derivation paths drift apart. That objection
is real, but it is a **testing** problem, not a design problem: a parity test that runs the same
aggregate through both paths and compares the resulting rows fails in CI the moment they disagree.

## Decision

**The live path stays event-based and unchanged. A second, explicitly invoked path rebuilds a
state-stored read model from the current aggregate state.**

- `IReadModelRebuilder<TAggregate, TKey>` (in `BuildingBlocks.Application.ReadModels`) has
  `ClearAsync` and `RebuildAsync(aggregate)`. It is a **multi-handler** contract: a context may
  register several rebuilders for the same aggregate, one per read model.
- `StateStoredReadModelRebuildRunner<TContext>` (in `BuildingBlocks.Infrastructure.ReadModels`)
  clears once, then streams the aggregate states out of the write `DbContext` with `AsNoTracking`,
  rehydrates each one through `AggregateFactory` plus `IStateOwner.Restore`, and hands the
  aggregates to the rebuilders in batches of 500, each batch in its own scope. It is registered by
  `UseEfCorePersistence` and is `public` so a migration worker can construct it without the full
  wiring — which is also why it lives in `ReadModels/` and not under `Persistence/StateStored/`,
  where `PublicSurfaceTests.NoInfrastructureImplementationIsPublic` forbids a public type.
- **Every read-model field must be a function of the current aggregate state.** A rebuilder writes
  absolute values (`PartCount = parts.Count`), never increments. A field that cannot be derived from
  the state — "how often was this renamed **last month**" — does not belong in a state-stored read
  model; the context needs event sourcing for it.
- The rebuild does **not** run through `DomainEventPublisher`, so it publishes no integration events
  and produces no cross-context replay.
- A rebuild is invoked explicitly, by the context's migration worker behind a configuration switch.
  It is not automatic and not incremental.
- **The runner throws when no rebuilder is registered.** A rebuild that projects nothing would
  report success while the read model stays empty — the same reasoning as
  `IntegrationEventMapperCheck`.
- **A parity test is mandatory** wherever a context has both projections and rebuilders: run one
  aggregate's events through the live projections, the same aggregate's final state through the
  rebuilders, and assert both rows are identical.

## Consequences

- ADR-0022's rebuildability promise now holds on both persistence paths, by different means.
- The handover from rebuild to live traffic needs no new mechanism. The rebuilder writes the
  aggregate's current `Version` as the watermark, and the existing `existing.Version < metadata.Version`
  guard in every projection handler then discards everything already contained and lets newer events
  continue incrementally.
- A rebuild is **not** online. It clears the read model first, so it needs downtime or a blue/green
  read database. That is acceptable for a repair operation and is the price of not keeping a journal.
- The two derivation paths are only as consistent as the parity test that pins them. A context that
  adds a projection without adding the matching rebuilder branch loses that guarantee silently —
  there is deliberately no start-up check for it, because "several rebuilders" and "no rebuilder"
  are both legitimate.
- The event-sourced path has a source for a rebuild (the stream) but no tool: no equivalent runner
  exists yet. Tracked as a separate low-priority TODO.
- Historisation stays out of scope. A context that needs to answer questions about the past switches
  to event sourcing (ADR-0012) — with the known limitation that the history then begins at the
  switch.

## Rejected alternatives

- **Domain-event journal** — rebuilds event sourcing without its benefits; permanent storage and
  retention obligation; a replay drags TODO-14 part B into scope.
- **Deriving the live path from state** — devalues the domain event for projections and moves read
  load onto the write database, to avoid a divergence a test already catches.

## Amendment (2026-08-10): the event-sourced path gets the same tool

`EventSourcedReadModelRebuildRunner` closes the gap the consequences above left open, and it does
so **without a second contract**: `IReadModelRebuilder<TAggregate, TKey>` is unchanged, and both
runners share the internal `ReadModelRebuildWriter`, which owns rebuilder resolution, the
"no rebuilder registered" throw, and the batching. Only the **source** differs, and that is the
whole point of the two paths:

- The state-stored runner reads the current state out of the write `DbContext`.
- The event-sourced runner asks Marten for the distinct stream keys carrying the aggregate's
  `[AggregateName]` prefix (`gadget/`), fetches each stream and folds it through the aggregate's own
  `LoadFromHistory` — the same rehydration `MartenEventSourcedRepository` performs, so a rebuilt
  aggregate is indistinguishable from a loaded one.

Two properties follow and are pinned by tests:

- **A rebuilt aggregate carries no uncommitted domain events.** The fold is a replay, not a
  command, so nothing is published and the rebuild stays as free of side effects as the
  state-stored one.
- **The stream-key prefix isolates aggregates by name, not by CLR type.** The separator in
  `EntityKeyFormatter.GetStreamKey` is `/`, which kebab-case names cannot contain, so
  `rebuild-probe/` never matches `rebuild-probe-neighbour/`. That is a persistence contract, not an
  implementation detail — renaming an `[AggregateName]` still breaks a rebuild exactly as ADR-0030
  says it breaks everything else.

The former `ReadModelRebuildRunner<TContext>` is renamed to `StateStoredReadModelRebuildRunner<TContext>`.
Once two runners exist, the unqualified name would suggest one is *the* runner and the other a special
case, when they are peers. The parity-test requirement now applies to both samples:
`GadgetReadModelRebuildTests` mirrors `WidgetReadModelRebuildTests`.

**Rejected here: replaying the stream through `ProjectionRunner`.** It looks cheaper — the same
derivation path as live traffic, so no parity test would be needed — but it buys that by making the
event-sourced rebuild a different *shape* of operation than the state-stored one. It has no place
to clear the read model first, so it would still need a second contract for that; a service with
one context of each kind would then write two unrelated kinds of rebuild code; and the "same path"
argument is weaker than it sounds, because a replay through the live handlers hits the watermark
guard on a row it just wrote. One contract on both paths, paid for with one parity test per sample,
is the better trade.
