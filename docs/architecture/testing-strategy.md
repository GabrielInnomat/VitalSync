# Testing Strategy

Automated tests are implemented for **both the Building Blocks and the individual microservices**. The strategy includes, but is not limited to, the categories below.

## Test categories

| Category                          | Purpose                                                         | Typical scope                                                 |
| --------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------- |
| **Unit tests**                    | Verify individual units in isolation                            | A class or method                                             |
| **Domain tests**                  | Verify domain rules, invariants, and event-raising              | Aggregates, value objects, domain events                      |
| **Application-layer tests**       | Verify command/query handlers and pipeline behaviors            | Handlers, dispatcher, pipeline, CQRS flow, `Result` semantics |
| **Persistence tests**             | Verify mapping, persistence, and event collection on save       | EF Core `DbContext`, converters                               |
| **Integration tests**             | Verify components working together with real-ish infrastructure | Service + database / messaging                                |
| **Component communication tests** | Verify messaging and contracts between components               | gRPC contracts, message publish/consume                       |

## Tooling

| Tool                 | Use                                                      |
| -------------------- | -------------------------------------------------------- |
| **xUnit**            | Test framework and assertions (`Assert.*`; see ADR-0014) |
| **NSubstitute**      | Mocking/substitutes                                      |
| **EF Core InMemory** | Fast persistence-layer tests                             |
| **Testcontainers (PostgreSQL)** | Integration tests against a real PostgreSQL for Marten optimistic concurrency, strongly-typed key persistence, aggregate child collections (owned types), and outbox flush-on-commit; skipped automatically when Docker is unavailable |
| **Testcontainers (RabbitMQ)** | Integration tests for integration-event routing to the platform topic exchange, for the durability of that delivery (persistent message, durable sending endpoint, quorum queues including the dead-letter queue), and for the context rules (start-up refusal of an unreachable or own-context handler, suppression of self-published events by their source header); skipped automatically when Docker is unavailable |
| **Smoke tests over gRPC**     | End-to-end checks against a running system (Aspire host, real broker, real databases); skipped unless the service's `SAMPLE_*_API_URL` is set |

> Integration and component-communication tests may additionally use containerized infrastructure (e.g., via Testcontainers) once the messaging platform is selected.

### Container-backed tests must not skip in CI

Skipping keeps the suite usable without Docker, but a build agent without Docker reports **success** while entire test classes never ran — the regressions they guard would land unnoticed. Setting the environment variable **`VITALSYNC_REQUIRE_CONTAINERS`** turns a failed container start into a failed run instead of a skip:

```bash
VITALSYNC_REQUIRE_CONTAINERS=1 dotnet test
```

Set it in every CI pipeline; leave it unset locally. Both fixtures (`PostgreSqlFixture`, `RabbitMqFixture`) honour it via `ContainerRequirement`.

## Continuous integration

[`.github/workflows/build.yml`](../../.github/workflows/build.yml) runs on every push to `main`, on pull requests, and on demand. In order:

1. **Build** in Release. `TreatWarningsAsErrors` and `AnalysisMode=All` from `Directory.Build.props` make this the analyzer and style gate as well.
2. **Test** the whole solution with `VITALSYNC_REQUIRE_CONTAINERS=1`, so a runner without Docker fails instead of skipping.
3. **Smoke-test a running system**: the workflow starts the samples AppHost, waits for both sample APIs, and re-runs the two sample test projects with `SAMPLE_STATESTORED_API_URL` / `SAMPLE_EVENTSOURCED_API_URL` and **`VITALSYNC_REQUIRE_SMOKE=1`** set. On failure the `Diagnostics` step prints the AppHost log — Wolverine's interesting failures (an unroutable message, a consumer that was never discovered) are invisible in test output but plain in the host log — plus `docker ps --all`, `docker images` and `docker logs --tail 200` per container. The container dump exists because Aspire routes resource logs to the dashboard, not to the AppHost's stdout: when a container stalls, `apphost.log` shows a clean start followed by silence and names no culprit. For the same reason the wait loop prints a container status line every sixth attempt, so a stall is visible while it happens instead of only afterwards.

`VITALSYNC_REQUIRE_SMOKE` is to the smoke tests what `VITALSYNC_REQUIRE_CONTAINERS` is to the container-backed ones: a missing API URL normally skips the test, which keeps the suite usable locally but would let a renamed variable turn the whole smoke stage into a green no-op. With the flag set, a missing URL fails (`SmokeRequirement`, one per sample test project).

Both test stages run with `--report-xunit-trx`, and each has an `if: failure()` step that prints every TRX entry with `outcome="Failed"`, message and stack trace included. This is not redundant with the uploaded artifact: the console output names only the *count* of failures ("Failed: 1, Passed: 244"), never the name. Before the TRX report the name existed solely inside a 668 KB TestResults log in the artifact, so every red run cost a download — and for runs old enough that the artifact had expired, the information was unrecoverable. That is what made one flaky test look for weeks like an unspecified CI problem. Keep flag and step together; either alone is useless.

Step 3 exists because the walking skeleton found several defects that build and unit tests stayed green through; only a real web host with a real broker surfaces them. It runs against `samples/` today, and when the first real service arrives only the project paths and the two URLs change.

The SDK is pinned by `global.json` (`rollForward: latestFeature`). No Aspire workload is installed — the AppHosts reference `Aspire.AppHost.Sdk` as a package.

> NSubstitute is used in the application/persistence/messaging tests; domain tests use lightweight hand-written test doubles instead.

## Principles

- **The domain is highly testable** because it has no infrastructure dependencies — domain tests need no mocks for frameworks.
- **Behavior over implementation** — assert observable behavior (e.g., "creating a recipe raises a `RecipeCreated` event") rather than internal details.
- **Read-only event access is enforced and tested** — outside layers must not be able to mutate an aggregate's domain events.
- **Fast feedback first** — unit/domain/application/persistence tests run quickly; heavier integration tests run as needed.
- **A negative assertion needs an anchor, not a deadline.** "Nothing was delivered" is never provable in a distributed system by waiting a fixed interval: too short and the test passes for the wrong reason, too long and it is slow — and on a loaded two-core runner the same code flips between the two. Anchor the assertion to an observable terminal state instead. Two shapes work here: a **terminal state** (a message sitting in the dead-letter queue means the handler has finally given up, so the attempt count is settled — no extra wait needed), and a **sentinel** (publish a second, healthy message afterwards, wait deterministically for *its* delivery, then assert that only it arrived; anything sent earlier would have arrived first). `IntegrationEventSinkDeliveryTests` uses the sentinel and `DeadLetterTests` the terminal state.
  Two things this rule does *not* condemn: a `Task.Delay` used as the **polling interval** inside a loop that checks a condition and fails on a deadline is the correct shape, not the criticised one — every wait in the sample smoke tests is of that kind. And a bounded wait that *supplements* an anchor is fine: `IntegrationEventSubscriptionValidationTests` anchors its negative assertion on a `control` message published from a foreign context **after** the suppressed one, so a single topic exchange and a single queue give the two a FIFO order: once the control arrives, the suppressed message has provably already been handled or discarded, and the short settling window that follows only guards against a late duplicate.
- **A broker test names its queue *and its exchange* per run.** All broker test classes share one RabbitMQ container and one PostgreSQL container through `[Collection(BrokerAndDatabaseCollection.Name)]`, and the queues are durable with `autoDelete: false`, so a fixed name outlives the run that created it and keeps collecting messages bound by another class's pattern. Use `TestMessaging.UniqueQueueName(prefix)` and `TestMessaging.UniqueExchangeName(prefix)`. The exchange matters for a reason that is easy to miss: a topic exchange fans one publication out to *every* bound queue **under one envelope id**, and all probe hosts share the shared container's `wolverine_incoming_envelopes` — so the durable inbox deduplicates the copies and exactly one host, whichever wins the race, sees the message. A test that shares an exchange with a still-running host of another test therefore fails with "the consumer never saw the message", and it fails only sometimes. One exchange per test removes the fan-out entirely.
- **A race is staged, not awaited.** A test that needs two writers to collide must not produce the collision with sleeps or parallel tasks — the interleaving then depends on the runner's mood, and the test passes for the wrong reason as often as it fails for one. Stage it instead: a pipeline behavior registered at `UnitOfWorkBehaviorOrder + 100` sits **inside** the unit of work, so its `NextAsync` returns after the handler has loaded and mutated the aggregate but before the commit — exactly the window a competitor has to hit. `ConcurrencyConflictScenarioTests` writes the competing change there, in its own DI scope, and the conflict is then a certainty rather than a probability. The handler under test stays untouched, which is the second benefit: no test hook leaks into production code.

## Where tests live

Test projects mirror the source structure 1:1 and sit in the `tests/` folder next to what they
cover: `BuildingBlocks/tests/<Package>.Tests` for the Building Blocks, and `tests/<Project>.Tests`
for everything under `src/` (e.g. `tests/VitalSync.ServiceDefaults.Tests`). Every test project
belongs in `VitalSync.slnx`, otherwise `dotnet test` never sees it.

`VitalSync.ServiceDefaults.Tests` disables xUnit's parallelisation
(`[assembly: CollectionBehavior(DisableTestParallelization = true)]`). Its OpenTelemetry tests
attach an `ActivityListener`, which is **process-global**: two tests listening at once each see the
other's activities, and the resulting failure depends on scheduling. Where the state under test is
global, serialising the assembly is cheaper and more honest than pretending otherwise.

## External fixture assemblies

Some tests need types that live in a **separate compiled assembly** from the test project
itself — most notably assembly-scanning tests (`AddHandlersFrom(assembly)`, startup handler
validation), where scanning the test assembly would pick up unrelated test types and pollute
the results. For these cases:

- **Where:** always place the fixture project under
  `BuildingBlocks/tests/ExternalAssemblies/<FixtureName>/` — one small project per fixture
  scenario. Reference it from the consuming test project via `ProjectReference` and add it
  to `VitalSync.slnx` (under the `ExternalAssemblies` solution folder).
- **Naming:** keep folder and project names **short** (e.g. `ValidHandlersFixture`, *not*
  `BuildingBlocks.Infrastructure.Tests.ValidHandlersFixture`). Long duplicated names have
  broken the Windows 260-character `MAX_PATH` limit before (build failure on checkout and
  compile). The root namespace equals the project name.
- **Rules:** fixture projects set `IsPackable=false`, reference only what the scenario needs
  (typically `BuildingBlocks.Domain` / `BuildingBlocks.Application`), and contain **no tests**
  themselves.

Existing examples: `ValidHandlersFixture`, `ConflictingHandlersFixture`,
`OrphanRequestsFixture`, `AmbiguousRequestsFixture` (used by
`BuildingBlocks.Infrastructure.Tests` for handler registration and
startup-validation tests), `DeadLetterFixture` (messaging dead-letter tests), and
`HullFixture` (an aggregate without a parameterless constructor, for the
reconstitution startup-validation tests), `SchemaFixture` (a closed set of well-declared
events, so the persisted-schema rendering can be asserted for exact equality) and
`SchemaGapFixture` (an event without `[EventName]` and one without
`[IntegrationEventTopic]` — they cannot live in the test assembly, because every test that
scans it for domain events would then fail).

## What the current Building Blocks tests cover

- **Domain**: strongly typed id equality and compile-time distinctness, aggregate event raising/clearing, read-only exposure of domain events, entity identity equality, value object structural equality, and `RuleCheckerTests`, which pins that a `null` rule throws instead of counting as satisfied — including the case where the `null` sits *between* two rules, so the ones after it are provably no longer evaluated.
- **Application**: dispatcher routing to the correct handler, pipeline behavior ordering and execution, exception-to-`Result` translation (business-rule / domain-validation), `Result` / `Result<T>` success/failure semantics including the argument guards (`Success(null)`, a `null` or empty failure list, an undefined `FailureCategory`), and `PublicSurfaceTests`.
- **`PublicSurfaceTests` in `Domain` and `Application`**: both blocks are `public` end to end, so their namespaces are part of the published API — a file moved between folders changes every exported type's `FullName` and breaks every consumer at once. Each test pins the complete, ordinal-sorted list of exported type names, which catches a move, an accidental `public`, and a forgotten `internal` with one assertion. Mirrors the Infrastructure test of the same name, which pins the opposite property (almost nothing may be public).
- **`EntityKeyConstraintTests` in `Domain` and `Application`**: scans the exported generic type definitions **and their generic methods** and fails when a `TKey` parameter requires `IEntityKey` but not `IEquatable<TKey>` (ADR-0008 amendment 2026-08-05). The compiler catches this only once such a type passes its `TKey` on to one of the bases, so a fresh declaration that stops short would otherwise slip through — and a generic *method* never passes it on at all, which is why the type-only version of this detector was blind to `EntityKeyModelBuilderExtensions`-shaped declarations. Both tests carry **positive controls** — one fixture that omits the constraint on a type and one that omits it on a method — so a detector that silently finds nothing fails too.
- **`TransientIdentityTests` in `Domain`**: pins why `EntityBase.Equals` does *not* special-case an empty identity. It asserts the uncomfortable property itself (two fresh hulls compare equal, and a hull's hash code changes when it gains identity — so a hull must never enter a `HashSet`), then that each of the guards standing in its way throws, and finally that every way out of the domain (named factory, `Restore`, `LoadFromHistory`) yields an identified aggregate. The fourth guard, `AddAsync` with an empty identity, is pinned by `RepositoryEmptyIdentityGuardTests` in Infrastructure on both persistence paths.
- **Infrastructure**: the real `RequestSender` with a DI container (failure translation, unit-of-work commit/suppression and concurrency-conflict mapping, dispatcher and handler registration, the typed failure a short-circuiting behavior produces for both `Result` and `Result<T>`), serialization/mapping (`DomainEventEnvelopeSerializer` round-trip with typed ids, `EntityKeyFormatter`, entity-key converter registration, and the bare-value JSON format of a typed key — `EntityKeyJsonConverterTests` for the converter itself and `EntityKeyEventStreamFormatTests`, which asserts the raw `mt_events.data` column against a real PostgreSQL so the format is pinned where it is immutable rather than only in memory), persistence against a real PostgreSQL via Testcontainers (`MartenEventSourcedRepository` version arithmetic / optimistic concurrency, strongly-typed key persistence, and `EfCoreChildCollectionTests` for aggregate child collections — insert, key-stable update, delete, version advance on a child-only change, a two-level owned graph whose grandchildren are inserted, updated and deleted, an `OwnsOne` child cleared by `null`, a `ToJson()` collection, a change raised through a child entity that round-trips through the same owned graph, and the startup rejection of a state navigating to an independent entity type and of a stored field name left to convention; skipped when Docker is unavailable), messaging against a real RabbitMQ via Testcontainers (`IntegrationEventRoutingTests` for routing and the marker rule, `DeadLetterTests` for the poison-message path — all three tests: an unknown failure is retried and then dead-lettered (asserted as a **lower** bound, because redelivery may add attempts), a deterministic one (`DomainValidationException`) is dead-lettered after a **single** attempt, which is the assertion that would silently pass under the old single-rule policy if the graded rules were ever removed, and a **transient** one (`TimeoutException`) is retried and provably **not** dead-lettered anywhere in a 15-second window — the rule whose whole point is that a failover outlasts any cooldown ladder, and the one no test covered before; the dead-letter queue is inspected **non-destructively** (`BasicGet` with `autoAck: false`, then `BasicNack` with `requeue: true`), because it is shared platform-wide and a draining read would delete another test's evidence, `IntegrationEventSubscriptionValidationTests` — whose two delivery tests rely on the FIFO order of one exchange and one queue rather than on republishing until an anchor arrives — `InboxDeduplicationTests` — which republishes one captured envelope, byte for byte and message id for message id, twice into the queue and asserts the handler ran exactly once, so Wolverine's durable inbox is pinned as a *behaviour* rather than as a configured number and `IntegrationEventDurabilityTests` — a published event arrives with `Persistent == true`, the compiled sending endpoint is `EndpointMode.Durable`, and the subscriber queue and the `wolverine-dead-letter-queue` are quorum and durable), and architecture tests enforcing the layer-dependency rules plus `PublicSurfaceTests`, which pins the assembly's exported types so `internal`-by-default cannot erode unnoticed, `DomainEventEnvelopeFactoryTests`, which pins the single place where an event's identity and per-aggregate version are minted (one `IClock.Now` per commit, consecutive versions ending at the aggregate's current version) — a mutation of that arithmetic is caught only there and by `MartenUnitOfWorkTests`, since no projection test asserts the watermark it depends on, and `StartupCheckRunnerTests`, which pins the two start-up phases and — with a real `Host` and a hosted service registered *after* the runner — the .NET guarantee the `AfterHostedServicesStarted` phase rests on.
- **`ConcurrencyConflictScenarioTests` (TODO-25)**: the only test that runs the whole optimistic-concurrency chain, for both persistence paths, against a real PostgreSQL — a command enters through `ISender`, loses the race for its aggregate and returns a `Result` carrying `FailureCategory.Conflict`. Every link was already pinned separately (`EfCoreAggregateRoundTripTests` and `MartenEventSourcedRepositoryTests` for the exceptions, `UnitOfWorkBehaviorTests` for the translation of *injected* ones); what was missing is that the exceptions genuinely arise in a wired host and reach the caller. The negative anchor is the reload afterwards: it asserts the **winner's** state and version, so a run in which both writers succeeded — or in which the conflict came from somewhere else entirely — cannot stay green.
- **`PersistedSchemaTests` in Infrastructure and in both samples (ADR-0035)**: the Infrastructure test renders `SchemaFixture` for exact equality and pins the three properties the snapshot rests on — the *effective* JSON name (a `[JsonPropertyName]` is reported, the CLR name is not), a typed key rendered as the bare value it serializes to, and `Verify`'s behaviour around the baseline (pass, stale `*.received.txt` removed, missing baseline, a path that cannot carry a rendering). Each sample then compares its **real** events against a checked-in `EventSchema.approved.txt`, which is where a field rename in production code actually turns the build red.
- **The inbox idempotency window in `WolverineExtensionTests` (ADR-0023 amendment 2026-08-06)**: three assertions, and the middle one is the point. That the window is applied when a persistence strategy was selected, that it is left untouched without one, and that its value is provably **different** from `new DurabilitySettings().KeepAfterMessageHandling` — without that third test the first would keep passing if Wolverine ever adopted the same number as its own default, and the guarantee would silently be back to being inherited rather than chosen.
- **`IntegrationEventMapperCheckTests` (ADR-0023 amendment 2026-08-06)**: four assertions, and the last one carries the design. A mapper without a transport fails naming the mapper; no mapper passes; a mapper with a transport passes; and **a mapper plus a host-supplied sink factory passes**. That fourth case is why the check probes the resolved `IIntegrationEventSinkFactory` instead of the wiring settings — `IntegrationEventSinkDeliveryTests` builds exactly such a host, and the wiring-based phrasing would have failed it for a configuration in which nothing is silently dropped.
- **`OwnContextIntegrationEventFilterTests` (ADR-0023 amendment 2026-08-05)**: the self-consumption filter is the one place where discarding a message is *correct*, so its edge cases decide between a lost event and an infinite loop. Six in-memory cases, no broker: the own context stops, a foreign one continues, and — the ones that matter — a **missing**, empty or blank source header continues, because a message from a producer that predates the header must never be dropped, and a casing lookalike continues rather than matching loosely.
- **`OutboxFlushOnCommitTests` also asserts the `DomainEventMetadata` that reaches the projection handler**: the watermark every projection's idempotency rests on is minted in `DomainEventEnvelopeFactory` and consumed by the samples' handlers — but the stretch in between (envelope, serialisation, outbox, `ProjectionRunner`) fell between the two tests. If `Version` were lost there, *every* watermark test in the samples would be silently pointless and none of them would turn red. Both persistence paths therefore assert `AggregateName`, `AggregateId`, `Version` and a populated identity at the point where the metadata is actually consumed.
- **The architecture tests read `project.assets.json`, not `Assembly.GetReferencedAssemblies()`**: the C# compiler drops a reference nobody uses, so an assembly-based check on `Domain` and `Application` was vacuously green — it would have kept passing after an EF Core package was added and left unused, which is exactly the moment the layering rule starts to erode. The assets file is the restored package graph, so it also sees transitive packages. A third test greps `BuildingBlocks/src` for the string `vitalsync`, which is what makes ADR-0018's independence promise checkable rather than aspirational.

## Related

- [Building Blocks](./building-blocks.md)
