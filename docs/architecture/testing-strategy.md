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
  Two things this rule does *not* condemn: a `Task.Delay` used as the **polling interval** inside a loop that checks a condition and fails on a deadline is the correct shape, not the criticised one — every wait in the sample smoke tests is of that kind. And a bounded wait that *supplements* an anchor is fine: `IntegrationEventSubscriptionValidationTests` anchors its negative assertion on a `control` message published from a foreign context and only then drains the queue. That class also republishes in a loop until its anchor arrives, which is a **retry against a reproduced delivery race**, not a timing cushion: Wolverine returns from `StartAsync` before the exchange-to-queue binding exists, and even an explicit `QueueBindAsync` plus a durable sending endpoint did not make a single publish reliable (see `todo.md`, TODO-37).
- **A broker test names its queue per run.** All broker test classes share one RabbitMQ container through `[Collection(BrokerAndDatabaseCollection.Name)]`, and the queues are durable with `autoDelete: false`, so a fixed name outlives the run that created it and keeps collecting messages bound by another class's pattern. Use `TestMessaging.UniqueQueueName(prefix)`. Hold it in a `readonly` **instance** field, never a `static` one: xUnit creates a new class instance per test, which is exactly the isolation wanted.

## Where tests live

Test projects mirror the source structure 1:1 and sit in the `tests/` folder next to what they
cover: `BuildingBlocks/tests/<Package>.Tests` for the Building Blocks, and `tests/<Project>.Tests`
for everything under `src/` (e.g. `tests/VitalSync.ServiceDefaults.Tests`). Every test project
belongs in `VitalSync.slnx`, otherwise `dotnet test` never sees it.

> `tests/VitalSync.Tests/` is a leftover from the Aspire template with a broken project reference
> and stale resource names. It is deliberately **not** in the solution; do not extend it.

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
- **`EntityKeyConstraintTests` in `Domain` and `Application`**: scans the exported generic type definitions and fails when a `TKey` parameter requires `IEntityKey` but not `IEquatable<TKey>` (ADR-0008 amendment 2026-08-05). The compiler catches this only once such a type passes its `TKey` on to one of the bases, so a fresh declaration that stops short would otherwise slip through. The Domain test carries a **positive control** — a fixture type that deliberately omits the constraint — so a detector that silently finds nothing fails too.
- **`TransientIdentityTests` in `Domain`**: pins why `EntityBase.Equals` does *not* special-case an empty identity. It asserts the uncomfortable property itself (two fresh hulls compare equal, and a hull's hash code changes when it gains identity — so a hull must never enter a `HashSet`), then that each of the guards standing in its way throws, and finally that every way out of the domain (named factory, `Restore`, `LoadFromHistory`) yields an identified aggregate. The fourth guard, `AddAsync` with an empty identity, is pinned by `RepositoryEmptyIdentityGuardTests` in Infrastructure on both persistence paths.
- **Infrastructure**: the real `RequestSender` with a DI container (failure translation, unit-of-work commit/suppression and concurrency-conflict mapping, dispatcher and handler registration, the typed failure a short-circuiting behavior produces for both `Result` and `Result<T>`), serialization/mapping (`DomainEventEnvelopeSerializer` round-trip with typed ids, `EntityKeyFormatter`, entity-key converter registration, and the bare-value JSON format of a typed key — `EntityKeyJsonConverterTests` for the converter itself and `EntityKeyEventStreamFormatTests`, which asserts the raw `mt_events.data` column against a real PostgreSQL so the format is pinned where it is immutable rather than only in memory), persistence against a real PostgreSQL via Testcontainers (`MartenEventSourcedRepository` version arithmetic / optimistic concurrency, strongly-typed key persistence, and `EfCoreChildCollectionTests` for aggregate child collections — insert, key-stable update, delete, version advance on a child-only change, a two-level owned graph whose grandchildren are inserted, updated and deleted, an `OwnsOne` child cleared by `null`, a `ToJson()` collection, a change raised through a child entity that round-trips through the same owned graph, and the startup rejection of a state navigating to an independent entity type and of a stored field name left to convention; skipped when Docker is unavailable), messaging against a real RabbitMQ via Testcontainers (`IntegrationEventRoutingTests` for routing and the marker rule, `DeadLetterTests` for the poison-message path — both classes: an unknown failure is retried four times and then dead-lettered, a deterministic one (`DomainValidationException`) is dead-lettered after a **single** attempt, which is the assertion that would silently pass under the old single-rule policy if the graded rules were ever removed; each test declares a **uniquely named queue** per run, because several probe queues bind the same `upstream.*` pattern and a reused durable queue accumulates messages published by earlier tests, `IntegrationEventSubscriptionValidationTests` — whose two delivery tests **publish in a loop until a positive control arrives** rather than publishing once and waiting, because a topic exchange silently drops a message published before the queue is bound, which would make the suppression test pass for the wrong reason — and `IntegrationEventDurabilityTests` — a published event arrives with `Persistent == true`, the compiled sending endpoint is `EndpointMode.Durable`, and the subscriber queue and the `wolverine-dead-letter-queue` are quorum and durable), and architecture tests enforcing the layer-dependency rules plus `PublicSurfaceTests`, which pins the assembly's exported types so `internal`-by-default cannot erode unnoticed, `DomainEventEnvelopeFactoryTests`, which pins the single place where an event's identity and per-aggregate version are minted (one `IClock.Now` per commit, consecutive versions ending at the aggregate's current version) — a mutation of that arithmetic is caught only there and by `MartenUnitOfWorkTests`, since no projection test asserts the watermark it depends on, and `StartupCheckRunnerTests`, which pins the two start-up phases and — with a real `Host` and a hosted service registered *after* the runner — the .NET guarantee the `AfterHostedServicesStarted` phase rests on.
- **`PersistedSchemaTests` in Infrastructure and in both samples (ADR-0035)**: the Infrastructure test renders `SchemaFixture` for exact equality and pins the three properties the snapshot rests on — the *effective* JSON name (a `[JsonPropertyName]` is reported, the CLR name is not), a typed key rendered as the bare value it serializes to, and `Verify`'s behaviour around the baseline (pass, stale `*.received.txt` removed, missing baseline, a path that cannot carry a rendering). Each sample then compares its **real** events against a checked-in `EventSchema.approved.txt`, which is where a field rename in production code actually turns the build red.
- **The inbox idempotency window in `WolverineExtensionTests` (ADR-0023 amendment 2026-08-06)**: three assertions, and the middle one is the point. That the window is applied when a persistence strategy was selected, that it is left untouched without one, and that its value is provably **different** from `new DurabilitySettings().KeepAfterMessageHandling` — without that third test the first would keep passing if Wolverine ever adopted the same number as its own default, and the guarantee would silently be back to being inherited rather than chosen.
- **`IntegrationEventMapperCheckTests` (ADR-0023 amendment 2026-08-06)**: four assertions, and the last one carries the design. A mapper without a transport fails naming the mapper; no mapper passes; a mapper with a transport passes; and **a mapper plus a host-supplied sink factory passes**. That fourth case is why the check probes the resolved `IIntegrationEventSinkFactory` instead of the wiring settings — `IntegrationEventSinkDeliveryTests` builds exactly such a host, and the wiring-based phrasing would have failed it for a configuration in which nothing is silently dropped.

## Related

- [Building Blocks](./building-blocks.md)
