# 0037. Creating schema and broker topology is a role, not a start-up side effect

- **Status:** Accepted
- **Date:** 2026-08-09
- **Amended:** 2026-08-09 (`DeclarePassive` does not do what this ADR assumed — see the amendment at the end)

## Context

Three components create foreign infrastructure the first time a new deployment starts, without
anyone having approved it:

- **Marten** builds its event tables and indexes at runtime. `StoreOptions.AutoCreateSchemaObjects`
  defaults to `CreateOrUpdate`, and `PersistenceRegistrar.UseMarten` does not change it. The
  event-sourced sample's migration worker migrates **only** the read context
  ([Program.cs:15-16](../../../samples/EventSourced/VitalSync.Sample.EventSourced.MigrationService/Program.cs)),
  so the write side has no migration step at all.
- **Wolverine** builds its message storage (`wolverine_incoming_envelopes`,
  `wolverine_outgoing_envelopes`, `wolverine_dead_letters`) the same way;
  `WolverineOptions.AutoBuildMessageStorageOnStartup` inherits `CreateOrUpdate` from the JasperFx
  profile. This happens in the **write** database of every context, next to the aggregates.
- **RabbitMQ topology** is declared by `ApplyBuildingBlocksMessagingDefaults`, which calls
  `.AutoProvision()`
  ([WolverineOptionsExtensions.cs:71-79](../../../BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Wiring/WolverineOptionsExtensions.cs)),
  so exchange, queues and bindings appear on the broker on first start.

In the samples this is invisible and convenient. In production it means an application instance
holds `CREATE`/`configure` rights on a database and a broker it merely uses, and that a rolling
deployment can change shared infrastructure mid-flight, from whichever pod happens to start first.

The broker case is worse than the database case, because a queue's type is fixed at declaration: a
broker still holding a classic queue of the same name makes `AutoProvision` fail (ADR-0023,
amendment 2026-08-04). A deployment that may create infrastructure can therefore also block itself.

The forces:

- The three cases are the same decision. Answering them separately would leave two of three doors
  open and produce three switches nobody can keep consistent.
- The development loop must stay a single `dotnet run` on the AppHost. A provisioning story that
  requires a manual step before F5 will be worked around.
- Marten has no migration files. Its schema is derived from `StoreOptions`, so "hand-written SQL,
  reviewed like an EF migration" is not available without giving up Marten's own schema management.
- Aspire already expresses the ordering: every context has a migration worker, and the service
  starts only after it via `.WaitForCompletion(...)`. The missing piece is *what the worker does*,
  not *when it runs*.
- The critical failure is silence. A service that finds no schema and creates it looks healthy; a
  service that finds no schema and starts anyway would fail on the first request instead.

## Decision

**Exactly one host per bounded context may create infrastructure — its migration worker. Every
other host runs with creation switched off and fails at start when what it needs is missing.**

### One switch, three effects

`BuildingBlocksOptions` gains a single selection, mandatory nowhere and defaulted to the safe side:

```csharp
options.ProvisionInfrastructure(InfrastructureProvisioning.Never);
options.ProvisionInfrastructure(InfrastructureProvisioning.AtStartup);
```

`Never` is the default. The value is one closed value, not a set of flags, for the same reason
`PersistenceChoice` is (ADR-0027): the three effects are one decision.

| Component               | `Never`                                                  | `AtStartup`                        |
| ----------------------- | -------------------------------------------------------- | ---------------------------------- |
| Marten                  | `AutoCreateSchemaObjects = AutoCreate.None`              | `AutoCreate.CreateOrUpdate`        |
| Wolverine storage       | `AutoBuildMessageStorageOnStartup = AutoCreate.None`     | `AutoCreate.CreateOrUpdate`        |
| RabbitMQ                | no `AutoProvision()`; nothing is declared                 | `AutoProvision()` as today         |

Wolverine's own switches cover its message store and the broker topology at its start, but Marten
applies a schema change lazily, on first use. `AtStartup` therefore adds one step of our own:
`MartenSchemaProvisioner`, an `IStartupCheck` in the `BeforeHostedServicesStart` phase that calls
`ApplyAllConfiguredChangesToDatabaseAsync`, so the worker leaves behind a complete schema rather
than one that materialises document by document.

JasperFx's blanket setup pass (`services.AddResourceSetupOnStartup(...)`) is **not** used, although
it looks like exactly this feature. It runs as a hosted service alongside Wolverine's own start and
opens RabbitMQ channels concurrently with it; Wolverine 6.23 dereferences a channel that can still
be null at that moment (`RabbitMqListener.CreateAsync`), which surfaces as a bare
`NullReferenceException` in roughly a dozen messaging tests. Provision the one resource that needs
it and let each broker client provision its own.

### The migration worker owns provisioning, through the same wiring as its service

A bounded context names its persistence, its messaging and its domain-event assemblies **exactly
once**, in the extension method it already has — `AddSampleEventSourcedInfrastructure`,
`AddSampleStateStoredInfrastructure`, and one per production context. That method gains the
provisioning parameter and passes it through; it is not a second call site.

```csharp
public static IHostApplicationBuilder AddSampleEventSourcedInfrastructure(
    this IHostApplicationBuilder builder,
    string writeConnectionString,
    string readConnectionString,
    Uri rabbitMqUri,
    string exchangeName,
    InfrastructureProvisioning provisioning = InfrastructureProvisioning.Never)
```

The service takes the default; the migration worker passes
`InfrastructureProvisioning.AtStartup`. Everything else about the two hosts is identical by
construction, which is the point: the worker declares the schema and the topology that the service
then expects, so the two cannot disagree about a context name, a connection string or an event
assembly.

The worker keeps its existing EF Core `MigrateAsync` calls for the state-stored write context and
every read context, and the AppHost keeps `.WaitForCompletion(migrations)` — which now carries
content on both persistence paths, once the event-sourced worker also references
`eventsourced-write` and the messaging resource.

### A service says what it needs, and says it at start

With provisioning off, a missing prerequisite must surface at start, not on the first request:

- `InfrastructurePresenceCheck` (`AfterHostedServicesStarted`) asks the resolved message store
  whether its tables exist and throws, naming the connection, when they do not.
- `BrokerTopologyCheck` (`BeforeHostedServicesStart`) declares the exchange and the subscriber
  queue **passively** on a connection of its own and throws, naming the missing one, when the
  broker answers 404. See the amendment below for why this is a check of ours rather than a
  Wolverine setting.

Both follow the existing rule: register the check unconditionally, guard it internally, and probe
the built container (`IServiceProviderIsService`), never the `IServiceCollection`.

### Development stays one command

The samples' migration workers select `AtStartup`; the sample APIs do not. `dotnet run` on the
AppHost therefore behaves exactly as today, and the samples become the executable proof of the
production arrangement rather than an exception to it.

## Consequences

- A production database and broker can be handed to the application with restricted rights. Only
  the migration worker's identity needs `CREATE` / `configure`; the services need neither.
- The event-sourced path stops being asymmetric: both workers migrate both sides of their context,
  by different means but under one rule.
- The queue-type trap becomes a single-place problem. Only the worker declares topology, so a
  conflicting queue fails one job that can be re-run, instead of an unknown share of the fleet.
- A service now fails to start when infrastructure is missing. That is the point, and it is a
  behaviour change: a deployment order mistake becomes a red pod instead of a broken request an
  hour later.
- The worker resolves registrations it does not use — the read store, the query handlers, the
  gRPC-facing services stay in its container. The cost is a few unused objects; the benefit is that
  no host can hold a divergent copy of the context's wiring. The subscription is the interesting
  case and lands on the right side by itself: the worker declares the consumer queue and its
  bindings, which is exactly the resource a service running with `Never` must find already there.
- Marten's schema still is not reviewable ahead of time. `AutoCreate.None` moves the moment of
  creation into a controlled job; it does not turn it into a diff. Producing a patch script for
  review (`marten db-patch`) stays possible on top of this decision and is deliberately not
  decided here.
- Nothing changes for the read databases: they are EF Core migrations today and stay that way.

## Alternatives considered

- **Leave `AutoProvision` and `CreateOrUpdate` in place.** Cheapest, and the status quo. Rejected
  because the failure it protects against is not a crash but an unapproved change to shared
  infrastructure — precisely the class of silent production behaviour this backlog is prioritised
  by.
- **Three independent switches, one per component.** More flexible, and flexibility is the defect:
  the only combinations that make sense are all-on and all-off, and any other combination is a host
  that half-creates its own environment.
- **A separate provisioning tool outside the application** (SQL scripts plus a broker definitions
  file, applied by CI). Cleanest separation, and the one this ADR gives up. Rejected because Marten
  derives its schema from `StoreOptions` and Wolverine derives its topology from the routing rules:
  hand-maintained artefacts would have to be kept in step with code that can change them, with no
  compiler and no test in between. The migration worker is the same code, so it cannot drift.
- **Provisioning by environment** (`AutoCreate` on in Development, off elsewhere). Tempting, and it
  is what JasperFx's own profile mechanism offers. Rejected because the production path would then
  be exercised by nobody until production — the same blind spot the walking skeleton exposed with
  skipped container tests. The role split is executed identically in every environment.
- **Letting the worker call `AddBuildingBlocks` itself**, with its own persistence and messaging
  selection next to the service's. Simpler to read at the call site, and it keeps the context
  extension method free of a parameter only one caller uses. Rejected because the duplicated lines
  are precisely the ones whose divergence is invisible: a worker with a different context name or a
  forgotten `AddDomainEventsFrom` provisions a topology and an event mapping the service does not
  use, and nothing — no compiler, no start-up check — connects the two hosts well enough to notice.

## Amendment (2026-08-09) — `DeclarePassive` is not the mechanism; `BrokerTopologyCheck` is

The decision above assumed a missing exchange would fail a non-provisioning host's start because
Wolverine's exchange is declared with `DeclarePassive = true`. The implementation proved that wrong
on both counts, and the measurement is worth keeping.

Wolverine declares broker objects only inside `AutoProvision`: both `RabbitMqExchange.InitializeAsync`
and `RabbitMqQueue.InitializeAsync` are guarded by `if (_parent.AutoProvision …)`, and
`DeclarePassive` is read further down, inside the `DeclareAsync` those guards skip. With
provisioning off, nothing is declared at all — passively or otherwise — so the setting was dead
code. Measured against a real broker (Wolverine 6.23):

| Missing resource | Actual behaviour with `Never` |
| ---------------- | ------------------------------- |
| Queue            | start fails, but with a bare `AlreadyClosedException` — `code=404, NOT_FOUND - no queue '…'` — naming neither the host nor the remedy |
| Exchange         | **host starts, `PublishAsync` returns successfully, the exchange is never created, and no consumer ever sees the message** |

The exchange row is the exact failure shape this ADR exists to remove, so the promise above was
not merely imprecise, it was unkept. `DeclarePassive` is therefore removed and replaced by
`BrokerTopologyCheck` (`BeforeHostedServicesStart`), which opens a connection of its own and calls
`ExchangeDeclarePassiveAsync` and `QueueDeclarePassiveAsync`, then throws naming the missing
resource and the migration worker. It runs **before** Wolverine starts, so its message arrives
instead of the 404 rather than after it. `BrokerTopologyCheckTests` pins all three cases against a
real broker.

The general lesson, and the reason this is recorded rather than quietly fixed: a configuration flag
that is only *read* on a code path the same configuration disables is indistinguishable from a
working guarantee until something is actually missing. A test asserting the flag's value — which the
first implementation had — confirms nothing. Assert the behaviour against the real dependency.