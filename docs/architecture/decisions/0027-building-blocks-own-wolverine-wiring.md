# 0027. Building Blocks own the persistence and Wolverine wiring

- **Status:** Accepted
- **Date:** 2026-07-31
- **Amended:** 2026-08-01 (one exception for the EF Core outbox — see the note below)
- **Amended:** 2026-08-03 (Building Blocks owns the `UseWolverine` call; the exception no longer reaches the host)
- **Amended:** 2026-08-09 (a start-up check is async, and the runner also drives a provisioning step — see ADR-0037)

## Context

The transactional guarantee at the heart of ADR-0022/0023 — aggregate state and outbox entries commit in **one**
write-database transaction — silently depended on the host doing three things correctly:

1. **EF Core hosts** had to register their `DbContext` themselves via Wolverine's
   `AddDbContextWithWolverineIntegration<TContext>` (not plain `AddDbContext`). A host following the standard
   EF Core or Aspire path (`AddNpgsqlDbContext<TContext>`) would compile, run, and pass every functional test —
   while the outbox write ran **outside** the state transaction. The guarantee degraded silently.
2. Every host had to call the right combination of three `WolverineOptions` extension methods from its
   `UseWolverine` setup: `ApplyBuildingBlockDomainEventRouting` (always), `ApplyBuildingBlockEfCoreOutbox`
   (EF Core hosts), `ApplyBuildingBlockMessagingDefaults(rabbitMqUri)` (messaging hosts). Forgetting or
   mismatching a call again failed silently or late.
3. Forgetting `UseWolverine` entirely surfaced only when the first commit tried to resolve the outbox — at
   runtime, in production.

The asymmetry made this worse: `UseMartenEventSourcing(connectionString)` already registered Marten itself
(impossible to get wrong), while `UseEfCorePersistence<TContext>()` trusted the host. Documentation warned about
all three failure modes, but a guarantee that relies on documentation is not a guarantee (compare IMP-05's
startup validation: fail fast, don't document pitfalls).

> **Amendment (2026-08-01) — the EF Core outbox is the one thing a host must wire itself.**
> The first real consumer of Building Blocks (the walking skeleton under `samples/`) showed that this ADR
> cannot hold in full. Wolverine 3.0 forbids an `IWolverineExtension` resolved from the container from
> modifying the service collection:
>
> > *As of Wolverine 3.0, it's no longer supported to alter IoC service registrations through Wolverine
> > extensions that are themselves registered in the IoC container* → *The service collection cannot be
> > modified because it is read-only.*
>
> Both halves of the EF Core outbox do exactly that — `PersistMessagesWithPostgresql` and
> `UseEntityFrameworkCoreTransactions` — so applying them from `BuildingBlocksWolverineExtension` crashes a
> real ASP.NET Core host at startup. A state-stored host therefore calls **one** method itself:
>
> ```csharp
> builder.Host.UseWolverine(opts => opts.UseBuildingBlocksEfCorePersistence(writeConnectionString));
> ```
>
> Everything else — handler discovery, the durable domain-event queue, the RabbitMQ transport and its
> routing — is still applied automatically by the extension, and event-sourced hosts still need nothing at
> all (Marten supplies their message store through `IntegrateWithWolverine`).
>
> This replaces `EfCoreMessageStoreRegistration`, ~70 lines of reflection into Wolverine internals that tried
> to register the message store at composition time. It only ever solved half the problem: the service
> registrations, not the options-side middleware. The tests did not catch the other half because they build
> hosts in a way that leaves the service collection mutable — only a real web host is strict.

> **Amendment (2026-08-03) — the exception is back inside Building Blocks; the host names its write database once.**
> The 2026-08-01 amendment left the host repeating the write connection string: once in
> `UseEfCorePersistence(cs)` and once in its own `UseWolverine(opts => opts.UseBuildingBlocksEfCorePersistence(cs))`.
> Nothing compared the two, so two typos apart the outbox sat in a different database than the aggregates and the
> ADR-0022 atomicity guarantee was gone without a symptom (TODO-06).
>
> The Wolverine 3.0 restriction stands unchanged — it forbids a **container-registered** extension from touching the
> service collection, not a `UseWolverine` callback. So Building Blocks issues that call itself, from an
> `IHostApplicationBuilder` overload:
>
> ```csharp
> builder.AddBuildingBlocks(options => options.UseEfCorePersistence<NutritionWriteDbContext>(writeConnectionString));
> ```
>
> It calls `UseWolverine` when the selection needs a runtime, applies the EF Core outbox from the connection string
> **already recorded** by `UseEfCorePersistence`, and takes an optional `Action<WolverineOptions>` for host-specific
> transport settings. Wolverine allows exactly one `UseWolverine`, so a host using this overload must not call it
> again — that callback is the way in. The `IServiceCollection` overload and the public
> `UseBuildingBlocksEfCorePersistence(cs)` remain for hosts and tests that wire Wolverine themselves; only those
> still name the database twice.
>
> **Same-day follow-up:** that last sentence held for one commit. With the three EF Core integration tests moved
> to the builder overload, `UseBuildingBlocksEfCorePersistence` had no caller left outside Building Blocks, so
> `WolverineHostExtensions` is **deleted** and its two calls live inline in the `UseWolverine` callback. There is
> now exactly **one** way to wire the EF Core outbox, and no public API through which a host could name a second
> database. The `IServiceCollection` overload keeps working for handlers, Marten, and messaging — a state-stored
> host, however, must register through the host builder; on the other path the outbox is simply never applied and
> the startup validator fails the host.
>
> With this, point 3 below ("the one remaining failure mode") can no longer occur on the builder path, and the
> host contract stated at the end of the Decision section shrinks further: **`AddBuildingBlocks(…)` and nothing
> else** — no `UseWolverine()` call at all, for either persistence style.

## Decision

Make the Building Blocks own every piece of wiring they depend on — the host cannot get it wrong ("pit of
success"):

1. **`UseEfCorePersistence<TContext>(connectionString, configureContext?)` registers the context itself** via
   `AddDbContextWithWolverineIntegration<TContext>` on the Npgsql provider (PostgreSQL is the single relational
   engine, ADR-0020), mirroring `UseMartenEventSourcing`. The host passes the write-database connection string
   (ADR-0021) and optionally a callback for additional context configuration. Aspire hosts *enrich* the
   registration afterwards (e.g. `EnrichNpgsqlDbContext<TContext>`) instead of re-registering it.
2. **A registered `IWolverineExtension` applies the Wolverine defaults automatically.** Each `Use*` selection
   records what it needs in an internal `WolverineWiringSettings`; `AddBuildingBlocks` registers a
   `BuildingBlocksWolverineExtension` that Wolverine picks up from the container when the host calls
   `UseWolverine`. It applies exactly the right combination: domain-event routing whenever a persistence style
   was selected, the EF Core transactional middleware for state-stored contexts, and the RabbitMQ transport,
   retry, and dead-letter defaults when `UseWolverineMessaging(rabbitMqUri)` was selected (the broker URI —
   typically the Aspire-provided connection string — now flows in through that call). The three `Apply*`
   methods and the extension are `internal`; hosts have no Wolverine configuration surface left.
3. **A startup check catches the one remaining failure mode.** `UseWolverine` lives on the host builder and
   cannot be issued from an `IServiceCollection` extension. When a selected capability requires Wolverine, a
   hosted `WolverineWiringStartupValidator` verifies at startup that a Wolverine runtime is registered and
   otherwise fails the host with an actionable message.

The complete host contract shrinks to: `AddBuildingBlocks(options => …)` plus an empty `UseWolverine()` call.

## Consequences

- **The single-transaction guarantee no longer depends on host discipline.** Failure mode 1 is structurally
  impossible, failure mode 2 is automated away, failure mode 3 fails fast at startup.
- **Aspire integration loses its trap.** The idiomatic-looking `AddNpgsqlDbContext<TContext>` can no longer
  silently replace the Wolverine-integrated registration; the sanctioned pattern is *register through Building
  Blocks, enrich through Aspire*.
- **A service without persistence or without RabbitMQ stays supported**: selecting nothing registers no
  DbContext, requires no Wolverine, and keeps the no-op integration-event transport; in-context projections
  without a broker need only a persistence selection (the local durable queue needs no RabbitMQ).
- Hosts can no longer override the Building Block Wolverine defaults inline in `UseWolverine` *before* the
  extension applies (DI-registered extensions run after the inline configuration). A host needing different
  transport policies registers its own `IWolverineExtension` — an accepted trade-off for the guarantee.
- Breaking API change (`UseEfCorePersistence` and `UseWolverineMessaging` signatures); no service consumes the
  package yet, so nothing migrates.

## Alternatives considered

- **Keep the host-wired API and validate at startup only.** Catches mistakes late-but-safely, yet keeps three
  documented pitfalls that every future service must re-learn. Prevention beats detection where prevention is
  structurally possible; validation remains only where it is not (`UseWolverine` itself).
- **Accept a pre-configured `DbContextOptionsBuilder` from the host.** Leaves the host free to also register
  the context conventionally, recreating failure mode 1.
- **Take a connection-string *name* and resolve it from `IConfiguration`.** More Aspire-idiomatic, but couples
  the options builder to configuration and hides the dependency; the host passing
  `builder.Configuration.GetConnectionString(...)` explicitly keeps the seam visible and testable.

> **The start-up checks are not optional (amendment 2026-08-05).** The two switches
> `ValidateHandlersOnStart` and `ValidateWolverineOnStart` are removed; both checks now
> always run, and a third joins them (integration-event subscription coverage,
> [ADR-0023](./0023-wolverine-messaging-transport.md)).
>
> Every one of these checks exists because the failure it catches is otherwise silent.
> An opt-out is therefore a switch whose only effect is to restore a silent failure, and
> a default-on flag that nobody sets is configuration surface without a purpose. The
> accepted cost: a host that registers handlers outside the scanned assemblies now fails
> at start instead of at the first request. No such host exists. Should a legitimate one
> appear, optionality can come back deliberately — narrower than a global on/off switch.

> **Amendment (2026-08-06) — one contract for every start-up check.**
> The checks introduced here and by ADR-0023/0030/0031 had each grown into its own
> `IHostedService`, some registered conditionally, one of them reading the
> `IServiceCollection` at composition time. They now share the internal
> `IStartupCheck` contract (`StartupPhase Phase` + `void Run()`), and a single
> `StartupCheckRunner` — the only hosted service Building Blocks registers — drives
> them: `BeforeHostedServicesStart` checks in its `StartAsync`,
> `AfterHostedServicesStarted` checks in its `StartedAsync`. The types are renamed
> accordingly (`WolverineWiringStartupValidator` ? `WolverineRuntimeCheck`,
> `HandlerRegistrationStartupValidator` ? `HandlerRegistrationCheck`,
> `AggregateStateModelStartupValidator` ? `AggregateStateModelCheck`,
> `IntegrationEventSubscriptionStartupValidator` ? `IntegrationEventSubscriptionCheck`,
> `MissingUnitOfWorkStartupLogger` ? `UnitOfWorkPresenceCheck`).
>
> Two rules follow, and both remove a previously untestable assumption:
>
> 1. **A check is registered unconditionally and guards itself.** `WolverineRuntimeCheck`
>    and `IntegrationEventSubscriptionCheck` receive `WolverineWiringSettings` and return
>    early when their capability was not selected. Whether a check runs is now a property
>    of the check, not of the composition root.
> 2. **A check probes the built container, never the service collection.**
>    `UnitOfWorkPresenceCheck` asks the built provider, so a host that registers
>    `IUnitOfWork` *after* `AddBuildingBlocks` no longer gets the notice wrongly.
>
> **Only the phase is load-bearing, not the registration order.** Every check is a pure
> reader; none mutates state another one reads, so their relative sequence decides only
> which message a broken host sees first. The one real ordering requirement —
> `IntegrationEventSubscriptionCheck` must see Wolverine's compiled handler graph — is
> satisfied by the .NET host's three-pass start (all `StartAsync` complete before any
> `StartedAsync` begins), not by a registration index. `StartupCheckRunnerTests` pins
> that guarantee with a real host and a hosted service registered after the runner.

> **Amendment (2026-08-09) — the contract is `RunAsync`, and the runner drives one
> non-check.**
> ADR-0037 needs two things the synchronous contract above cannot express. First, a check
> that asks infrastructure whether it exists does real I/O: `InfrastructurePresenceCheck`
> calls `IMessageStoreAdmin.AssertStorageExistsAsync`. `IStartupCheck.Run()` therefore
> becomes `Task RunAsync(CancellationToken)`, and the runner awaits it in both phases.
> The six existing checks stay synchronous and say so by deriving from the new
> `SynchronousStartupCheck`, which implements the async member over a `protected abstract
> void Run()` — a body returning `Task.CompletedTask` per check would trip `IDE0046` six
> times and hide which checks actually wait on something.
>
> Second, the runner now also drives `MartenSchemaProvisioner`, which **writes**. That
> contradicts "every check is a pure reader" above, and the ordering rule survives it for
> a narrow reason: it is the only writer, it runs in `BeforeHostedServicesStart`, and no
> check in that phase reads what it creates (`InfrastructurePresenceCheck` runs in the
> later phase and returns early on exactly the hosts that provision). Adding a second
> writer would break that argument and needs a real ordering mechanism, not another
> `IStartupCheck`.

> **Amendment (2026-08-05) — `AddBuildingBlocks` is called exactly once, and a behavior
> without an order is a registration error.**
> Three of the objects the composition root registers are a single shared instance that
> the options lambda writes into: the `PipelineBehaviorRegistry`, the
> `WolverineWiringSettings`, and the `DomainEventTypeRegistry`. All three were registered
> with `TryAddSingleton`, so a second `AddBuildingBlocks` call on the same service
> collection *succeeded* — and lost everything that second call configured: its behaviors
> ran at order `0`, its persistence and messaging selection was ignored, and its
> `[EventName]` names were absent at the first commit. Three silent failures from one
> harmless-looking line.
>
> A second call now throws. The restriction costs nothing a host may legitimately want:
> a bounded context has one write database (ADR-0021), Wolverine permits one
> `UseWolverine`, and `AddDomainEventsFrom` is frozen by the `Validate` phase anyway.
> Sharing the state across calls instead (the originally proposed fix) would have been
> more code to enable a composition nobody needs. The three shared objects are registered
> with plain `AddSingleton` afterwards, so a foreign registration of the same type
> collides loudly instead of winning quietly.
>
> The second half of the same failure: `PipelineBehaviorRegistry.GetOrder` returned `0`
> for an unknown behavior, which is exactly `LoggingBehaviorOrder` — a behavior added
> straight to the `IServiceCollection` therefore shared a slot with the logging behavior
> and made the canonical order (ADR-0015) unpredictable, with no error anywhere. It now
> throws, and the new `ValidateBehaviorOrders` phase moves that failure from the first
> dispatched request to host start by scanning the service collection for
> `IPipelineBehavior<,>` descriptors whose implementation type the registry does not know
> (a factory-registered behavior is rejected too, its implementation type cannot be
> inspected). `options.AddPipelineBehavior(type, order)` is the only supported way to add
> a behavior, and the message says so. `CompositionSingleCallTests` pins both rules.

> **Amendment (2026-08-05) — committing nothing is a choice, not a default.**
> `UnitOfWorkBehavior` took `IUnitOfWork? unitOfWork = null` and skipped the commit when
> no unit of work was registered. That removed the original crash, but left the worst
> possible failure shape in its place: **the command reports success and the data is
> gone.** The only evidence was one `Information` log at start, which nobody reads in
> production.
>
> The dependency is now non-optional. A `NullUnitOfWork` fallback is registered in
> `RegisterCore` with `TryAddScoped`, so a real unit of work — from a persistence
> selection or from the host — always wins, and the pipeline always resolves.
> `UnitOfWorkPresenceCheck` turns from a logger into a hard check: it passes when a
> persistence strategy was selected, when the host registered its own `IUnitOfWork`, or
> when the scanned assemblies contain no commands at all, and it **throws**, naming the
> affected commands, when commands would be dispatched into the fallback.
>
> A host that genuinely commits nothing — a gateway, a facade, a host with its own
> persistence — states that with the new `UseNoPersistence()`. That is a **positive
> selection** on `PersistenceChoice`, not an opt-out flag in the sense of the 2026-08-05
> amendment above: it disables no check, it is mutually exclusive with
> `UseEfCorePersistence`/`UseMartenEventSourcing` (combining them throws), and the check
> still runs and logs the deliberate choice. `PersistenceChoice` gains a fourth case for
> it, which is why the choice now distinguishes `IsChosen` (something was said) from
> `IsSelected` (a real store exists, the fact that drives outbox, domain-event routing
> and Wolverine).
>
> This resolves the standing conflict between IMP-07 (which called the nullable
> parameter the fix) and hacky-11 (which called it the bug): both are satisfied, because
> the crash stays fixed *and* the silent path can only be reached by writing it down.
> `UnitOfWorkPresenceCheckTests` and `PersistenceChoiceTests` pin the rule.
