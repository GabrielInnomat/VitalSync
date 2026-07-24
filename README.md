# VitalSync

> A cloud-native, distributed platform unifying **nutrition**, **workout**, and **health analytics** in a single application.

VitalSync lets users manage nutrition- and workout-related information and derive meaningful insights from the collected data. It is built as a distributed system of independent microservices follo[...]

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

VitalSync combines three domains — **nutrition**, **fitness**, and **analytics** — behind a single, modern user experience. The platform is designed to be modular, extensible, maintainable, te[...]

A core principle of the project: **the architecture is fixed, the domain is fluid.** Technical decisions (communication mechanisms, layer separation, architectural principles) are mandatory. Busin[...]

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

| Concern                 | Choice                                                             |
| ----------------------- | ------------------------------------------------------------------ |
| Orchestration           | .NET Aspire 13                                                     |
| Frontend                | Blazor                                                             |
| Backend-for-Frontend    | REST (outbound) + code-first gRPC (to services)                    |
| Microservices           | ASP.NET Core, one per business area                                |
| Inter-service messaging | RabbitMQ via MassTransit (see ADR-0004)                            |
| Persistence             | Entity Framework Core; Event Sourcing where it adds business value |
| Patterns                | DDD, CQRS, Event Sourcing (selective)                              |
| Testing                 | xUnit (built-in asserts, see ADR-0014), NSubstitute, EF Core InMemory |

> **Note:** `.NET Aspire 13` is the chosen orchestrator version for this project. Aspire is applied at the orchestration/application layer; the reusable Building Blocks remain framework-agnostic.

## Repository structure

> The layout below reflects the current structure. Service subfolders are populated as the project grows.

```text
VitalSync/
├── BuildingBlocks/              # Reusable, VitalSync-independent platform
│   ├── src/
│   │   ├── BuildingBlocks.Domain/
│   │   ├── BuildingBlocks.Application/
│   │   ├── BuildingBlocks.Infrastructure/
│   │   ├── BuildingBlocks.Persistence/
│   │   ├── BuildingBlocks.EventProcessing/
│   └── tests/
│       ├── BuildingBlocks.Domain.Tests/
│       ├── BuildingBlocks.Application.Tests/
│       ├── BuildingBlocks.Persistence.Tests/
│       └── BuildingBlocks.EventProcessing.Tests/
├── src/                         # VitalSync application
│   ├── Aspire/                  # .NET Aspire AppHost & ServiceDefaults
│   ├── Bff/                     # Backend-for-Frontend
│   ├── Frontend/                # Blazor client
│   └── Services/                # One folder per microservice
│       ├── Nutrition/
│       ├── Fitness/
│       └── Analytics/
├── docs/                        # Architecture & decision records
└── tests/                       # Cross-cutting / integration tests
```

## Building Blocks platform

In addition to the application, VitalSync includes a **reusable platform of shared Building Blocks** providing reusable concepts for the Domain, Application, Infrastructure, Persistence, and Event Processing layers.

These Building Blocks are deliberately **independent of VitalSync** and reusable in future projects. See [Building Blocks](./docs/architecture/building-blocks.md).

## Getting started

> ⚠️ Prerequisites and run instructions will be expanded as the application code lands.

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version aligned with .NET Aspire 13)
- [.NET Aspire 13 workload](https://learn.microsoft.com/dotnet/aspire/)
- Docker (for local messaging infrastructure and containers)

### Build

```bash
dotnet build
```

### Run (Aspire AppHost)

```bash
dotnet run --project src/Aspire/VitalSync.AppHost
```

## Testing

The testing strategy includes (but is not limited to): unit, integration, domain, application-layer, persistence, and component-communication tests.

```bash
dotnet test
```

See [Testing strategy](./docs/architecture/testing-strategy.md).

## Documentation

- [Architecture overview](./docs/architecture/overview.md)
- [Communication](./docs/architecture/communication.md)
- [Building Blocks](./docs/architecture/building-blocks.md)
- [BuildingBlocks.Application reference](./docs/architecture/building-blocks-application.md)
- [Domain model](./docs/architecture/domain-model.md)
- [CQRS & Event Sourcing](./docs/architecture/cqrs-and-event-sourcing.md)
- [Testing strategy](./docs/architecture/testing-strategy.md)
- [Architecture Decision Records](./docs/architecture/decisions/README.md)
- [Glossary](./docs/glossary.md)

## Contributing

This is an evolving project. Architectural principles are mandatory; business/domain details are refined iteratively. When proposing changes that affect architecture, please add or update an [ADR[...]

---

_VitalSync — unify nutrition, activity, and health._
