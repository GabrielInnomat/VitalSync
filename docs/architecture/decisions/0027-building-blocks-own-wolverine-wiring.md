# 0027. Building Blocks own the persistence and Wolverine wiring

- **Status:** Accepted
- **Date:** 2026-07-31

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
   otherwise fails the host with an actionable message. Opt-out via
   `BuildingBlocksOptions.ValidateWolverineOnStart = false` (on by default, matching IMP-05's
   `ValidateHandlersOnStart`).

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
