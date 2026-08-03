# VitalSync

[![build](https://github.com/GabrielInnomat/VitalSync/actions/workflows/build.yml/badge.svg)](https://github.com/GabrielInnomat/VitalSync/actions/workflows/build.yml)

> A cloud-native, distributed platform unifying **nutrition**, **fitness**, and **health analytics** in a single application.

VitalSync lets users manage nutrition- and workout-related information and derive meaningful insights from the collected data. It is built as a distributed system of independent microservices following Domain-Driven Design, CQRS, and selective Event Sourcing.

> **Project status:** 🚧 Early development. Business requirements and domain models are intentionally refined iteratively. The technical architecture, however, is considered mandatory and stable.

---

## Table of contents

- [VitalSync](#vitalsync)
    - [Table of contents](#table-of-contents)
    - [Vision](#vision)
    - [Business domains](#business-domains)
        - [Nutrition](#nutrition)
        - [Fitness](#fitness)
        - [Analytics \& Reporting](#analytics--reporting)
    - [Architecture at a glance](#architecture-at-a-glance)
    - [Technology stack](#technology-stack)
    - [Repository structure](#repository-structure)
    - [Building Blocks platform](#building-blocks-platform)
    - [Getting started](#getting-started)
        - [Prerequisites](#prerequisites)
        - [Build](#build)
        - [Run (Aspire AppHost)](#run-aspire-apphost)
    - [Testing](#testing)
    - [Documentation](#documentation)
    - [Contributing](#contributing)

---

## Vision

VitalSync combines three domains — **nutrition**, **fitness**, and **analytics** — behind a single, modern user experience. The platform is designed to be modular, extensible, maintainable, testable, and independently deployable.

A core principle of the project: **the architecture is fixed, the domain is fluid.** Technical decisions (communication mechanisms, layer separation, architectural principles) are mandatory. Business requirements and domain models are refined iteratively as the project evolves.

## Business domains

### Nutrition

- Manage ingredients and their nutritional values
- Create recipes
- Compose meal plans
- Generate shopping lists
- Calculate nutrient intake based on consumed meals

### Fitness

- Manage exercises
- Create workout plans
- Track completed workout sessions
- Determine energy expenditure and calories burned

### Analytics & Reporting

- Reporting and analytical capabilities derived from nutrition and fitness data
- Specific analytics requirements are identified and extended throughout the project

> The final decomposition into Bounded Contexts is part of the project itself and is refined iteratively. See [Domain model](./docs/architecture/domain-model.md).

## Architecture at a glance

```text
            ┌─────────────┐
            │   Blazor    │   Frontend (UI only)
            └──────┬──────┘
                   │ REST (HTTP/JSON)
            ┌──────▼──────┐
            │     BFF     │   Backend-for-Frontend
            └──────┬──────┘
                   │ gRPC (code-first)
   ┌───────────────┼───────────────┐
   │               │               │
┌──▼───┐       ┌───▼────┐      ┌───▼─────┐
│Nutri-│       │Fitness │      │Analytics│   Microservices
└──┬───┘       └───┬────┘      └────┬────┘
   │               │                │
   └───────────────┴────────────────┘
         Asynchronous messaging only

```

**Communication rules:**

- The Blazor frontend communicates **exclusively** through the BFF.
- The BFF exposes **REST** to the frontend and talks to microservices via **code-first gRPC**.
- Microservices **never** call each other synchronously. All inter-service communication is **asynchronous** via a messaging platform.

See [Communication](./docs/architecture/communication.md) for details.

## Technology stack

| Concern                 | Choice                                                                                                                                           |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Orchestration           | .NET Aspire 13                                                                                                                                   |
| Frontend                | Blazor                                                                                                                                           |
| Backend-for-Frontend    | REST (outbound) + code-first gRPC (to services)                                                                                                  |
| Microservices           | ASP.NET Core, one per business area                                                                                                              |
| Inter-service messaging | RabbitMQ via Wolverine (see ADR-0023, supersedes ADR-0004)                                                                                       |
| Persistence             | EF Core on PostgreSQL (see ADR-0020); Event Sourcing via Marten on PostgreSQL where it adds business value (see ADR-0019)                        |
| Database topology       | PostgreSQL; a write + read database pair per bounded context (see ADR-0021); shared server now, server-per-context possible later (see ADR-0020) |
| Read models             | Event-driven projections in each context's read database via an outbox-backed publisher (see ADR-0022)                                           |
| Patterns                | DDD, CQRS, Event Sourcing (selective)                                                                                                            |
| Testing                 | xUnit (built-in asserts, see ADR-0014), NSubstitute, EF Core InMemory                                                                            |

> **Note:** `.NET Aspire 13` is the chosen orchestrator version for this project. Aspire is applied at the orchestration/application layer; the reusable Building Blocks remain framework-agnostic.

## Repository structure

> The layout below reflects the current structure. Service subfolders are populated as the project grows.

```text
VitalSync/
├── BuildingBlocks/              # Reusable, VitalSync-independent platform
│   ├── src/
│   │   ├── BuildingBlocks.Domain/
│   │   ├── BuildingBlocks.Application/
│   │   └── BuildingBlocks.Infrastructure/
│   └── tests/
│       ├── BuildingBlocks.Domain.Tests/
│       ├── BuildingBlocks.Application.Tests/
│       └── BuildingBlocks.Infrastructure.Tests/
├── src/                         # VitalSync application
│   ├── Aspire/                  # .NET Aspire AppHost & ServiceDefaults
│   ├── Bff/                     # Backend-for-Frontend
│   ├── Frontend/                # Blazor client
│   └── Services/                # One folder per microservice; each has an
│       ├── Nutrition/           #   Api and a MigrationService project
│       ├── Fitness/
│       └── Analytics/
├── samples/                     # Throwaway walking skeleton (own Aspire host)
├── docs/                        # Architecture & decision records
└── tests/                       # Cross-cutting / integration tests
```

## Building Blocks platform

In addition to the application, VitalSync includes a **reusable platform of shared Building Blocks** providing reusable concepts for the Domain, Application, and Infrastructure layers.

These Building Blocks are deliberately **independent of VitalSync** and reusable in future projects. See [Building Blocks](./docs/architecture/building-blocks.md) for the overview, and the per-package references:

- [BuildingBlocks.Domain](./docs/architecture/building-blocks-domain.md)
- [BuildingBlocks.Application](./docs/architecture/building-blocks-application.md)
- [BuildingBlocks.Infrastructure](./docs/architecture/building-blocks-infrastructure.md)

## Getting started

> ⚠️ Prerequisites and run instructions will be expanded as the application code lands.

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) — the version is pinned in `global.json` (`10.0.302`, rolling forward within the feature band)
- Docker (for local messaging infrastructure and containers)

> No Aspire workload install is needed: the AppHosts reference `Aspire.AppHost.Sdk` as a package, which CI confirms by building without one.

### Build

```bash
dotnet build
```

### Run (Aspire AppHost)

```bash
dotnet run --project src/Aspire/VitalSync.AppHost
```

The AppHost composes the whole system:

- **RabbitMQ** (`messaging`, management plugin) and **PostgreSQL** (`postgres`, pgAdmin), both with a data volume.
- A **write/read database pair per bounded context** — `nutrition-write` / `nutrition-read`, `fitness-write` / `fitness-read`, `analytics-write` / `analytics-read` — on that one shared server, per [ADR-0021](./docs/architecture/decisions/0021-write-read-database-pair-per-context.md).
- One **migration worker per context**, which runs to completion before its service starts (`WaitForCompletion`).
- The three **services**, the **BFF**, and the **Blazor frontend** (the only externally reachable endpoint), each gated on a `/health` check.

> The service and migration projects are still **skeletons without domain code**; the walking skeleton under `samples/` (see [WalkingSkeleton.md](./WalkingSkeleton.md)) has its own Aspire host and is what currently exercises the Building Blocks end to end.

## Testing

The testing strategy includes (but is not limited to): unit, integration, domain, application-layer, persistence, and component-communication tests.

```bash
dotnet test
```

Container-backed tests skip when Docker is unavailable. To make a missing Docker fail the run instead — as CI does — set `VITALSYNC_REQUIRE_CONTAINERS=1`.

Every push to `main` and every pull request runs [the build workflow](./.github/workflows/build.yml): build, the full test suite with containers required, and smoke tests against a running system started from the samples AppHost.

See [Testing strategy](./docs/architecture/testing-strategy.md).

## Documentation

- [Architecture overview](./docs/architecture/overview.md)
- [Communication](./docs/architecture/communication.md)
- [Building Blocks](./docs/architecture/building-blocks.md)
    - [BuildingBlocks.Domain](./docs/architecture/building-blocks-domain.md)
    - [BuildingBlocks.Application](./docs/architecture/building-blocks-application.md)
    - [BuildingBlocks.Infrastructure](./docs/architecture/building-blocks-infrastructure.md)
- [Domain model](./docs/architecture/domain-model.md)
- [CQRS & Event Sourcing](./docs/architecture/cqrs-and-event-sourcing.md)
- [Testing strategy](./docs/architecture/testing-strategy.md)
- [Architecture Decision Records](./docs/architecture/decisions/README.md)
- [Glossary](./docs/glossary.md)

## Contributing

This is an evolving project. Architectural principles are mandatory; business/domain details are refined iteratively. When proposing changes that affect architecture, please add or update an [ADR](./docs/architecture/decisions/README.md).

---

_VitalSync — unify nutrition, activity, and health._
