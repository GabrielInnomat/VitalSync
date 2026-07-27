# 0024. Contracts live in the innermost layer that consumes them

- **Status:** Accepted
- **Date:** 2026-07-27

## Context

With `BuildingBlocks.Infrastructure` being specified
([ADR-0018](./0018-three-building-block-packages.md),
[building-blocks-infrastructure.md](../building-blocks-infrastructure.md)),
several abstractions need a home whose implementations are framework-bound and
therefore live in `Infrastructure`:

- `IRepository<TAggregate, TKey>`
- `IUnitOfWork`
- the domain-event publisher and projection-handler abstractions
- the integration-event marker

Classic DDD literature places repository interfaces in the **Domain** layer,
treating "a Recipe repository" as domain vocabulary. Other Clean Architecture
practitioners place them in the **Application** layer. Without a fixed rule,
the question gets relitigated per contract.

The deciding observation for VitalSync's architecture:

- The Domain layer is **pure behavior** — aggregates, value objects, rules.
  Nothing *inside* the domain ever calls a repository or a unit of work:
  aggregates do not load other aggregates; only **command handlers** do.
- These contracts' signatures are inherently **async orchestration**
  (`Task`, `CancellationToken`) — use-case vocabulary, not domain vocabulary.
- Transactions (`IUnitOfWork`) and post-commit workflows (projections,
  publishing) are use-case concerns by definition.
- Integration events describe **cross-system communication**; the domain must
  not even know other services exist.

## Decision

**A contract belongs in the innermost layer whose language it speaks and that
actually consumes it** — decided by its consumer, not its implementor
(implementations always live outside, per Dependency Inversion).

Applied to the current contracts:

| Contract                                        | Home          | Reason                                                        |
| ----------------------------------------------- | ------------- | -------------------------------------------------------------- |
| `IDomainEvent`, business rules, `IClock`         | `Domain`      | Domain vocabulary, consumed by aggregates                       |
| `IRepository<TAggregate, TKey>`                  | `Application` | Only consumed by handlers; async orchestration signature        |
| `IUnitOfWork`                                    | `Application` | Transactions are a use-case concern                             |
| Projection-handler / event-publisher abstractions | `Application` | Post-commit workflow, CQRS concern                              |
| Integration-event marker                         | `Application` | Cross-system communication is not domain vocabulary             |
| All implementations                              | `Infrastructure` | Framework/third-party-bound, per DIP                         |

`BuildingBlocks.Domain` therefore stays BCL-only **and** free of persistence,
transaction, and messaging concepts. `BuildingBlocks.Infrastructure` defines
**no** use-case contracts of its own.

**Revisit trigger:** if a future domain service must look up aggregates while
enforcing an invariant (i.e. the Domain layer itself becomes a repository
consumer), the repository contract moves to `Domain` under this same rule —
via a superseding ADR.

## Consequences

- **Easier:** one stated rule decides placement for every future contract; the
  `IRepository` question is settled and does not get relitigated; the Domain
  layer declares no dependencies it never uses.
- **Uniform:** `Application` is confirmed as the contract layer for all
  orchestration-facing abstractions; `Infrastructure` remains implementations
  only.
- **Harder / accepted trade-offs:** deviates from the classic DDD convention of
  repository-interfaces-in-Domain, which contributors may expect; the revisit
  trigger above covers the case where that convention would become correct.

## Alternatives considered

- **Repository interfaces in `Domain` (classic DDD):** treats repositories as
  domain vocabulary, but in this architecture nothing in the Domain layer
  consumes them — the placement would be pure ceremony and would pull async
  orchestration signatures into the pure core. Rejected while the Domain layer
  has no repository consumers (see revisit trigger).
- **Contracts next to their implementations in `Infrastructure`:** would force
  services and `Application` code to reference `Infrastructure`, inverting the
  dependency rule and coupling use cases to the framework-bound layer.
  Rejected outright.
- **Per-contract, case-by-case placement without a rule:** maximally flexible
  but guarantees repeated debate and inconsistent placement over time.
  Rejected in favour of one stated principle.
