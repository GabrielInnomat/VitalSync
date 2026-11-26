# VitalSync

[![deploy](https://github.com/GabrielInnomat/VitalSync/actions/workflows/deploy.yml/badge.svg)](https://github.com/GabrielInnomat/VitalSync/actions/workflows/deploy.yml)

> A cloud-native, distributed platform unifying **nutrition**, **fitness**, and **health analytics**
> in a single application.

VitalSync lets users manage nutrition- and workout-related information and derive meaningful insights
from the collected data. It is built as a distributed system of independent microservices following
Domain-Driven Design, CQRS, and selective Event Sourcing.

## Documentation

Everything about how VitalSync works lives in [`docs/`](./docs/README.md).

- [Architecture](./docs/architecture.md) — tiers, communication rules, service anatomy, databases
- [Patterns](./docs/patterns.md) — DDD, CQRS, persistence, read models, integration events
- [Technologies](./docs/technologies.md) — what it is built on, and why
- [Testing](./docs/testing.md) — what is tested at which level
- [Glossary](./docs/glossary.md) — the vocabulary the documents share

Business domains: [Nutrition](./docs/domains/nutrition.md) ·
[Fitness](./docs/domains/fitness.md) · [Health Analytics](./docs/domains/health-analytics.md)

## Repository structure

```text
VitalSync/
├── src/
│   ├── Aspire/                  .NET Aspire AppHost & ServiceDefaults
│   ├── Bff/                     Backend-for-Frontend
│   ├── Frontend/                Blazor client
│   └── Services/                One folder per microservice, each with an Api and a MigrationService
│       ├── Nutrition/
│       ├── Fitness/
│       └── HealthAnalytics/
├── docs/                        Architecture documentation
├── tools/                       Build-time checks, hosting scripts and the operations guide, not shipped
└── tests/                       Cross-cutting / integration tests
```

## Getting started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) — the version is pinned in `global.json`
  (`10.0.302`, rolling forward within the feature band)
- Docker, for the local infrastructure and for container-backed tests

> No Aspire workload install is needed: the AppHost is a plain .NET project that references the
> `Aspire.Hosting.AppHost` package. The orchestrator and the dashboard come from the Aspire CLI
> bundle. The build uses a compatible `aspire` on the `PATH` and otherwise fetches the matching CLI
> through `dnx`, which ships with the .NET SDK.

### Build, run and test

```bash
dotnet build
dotnet run --project src/Aspire/VitalSync.AppHost
dotnet test
```

The AppHost composes the whole system — see [Architecture](./docs/architecture.md). With the Aspire
CLI installed, `aspire run` from the repository root starts the same AppHost.

## Contributing

Architectural principles are mandatory; business and domain details are refined iteratively. When a
change affects the architecture, update the affected document under [`docs/`](./docs/README.md).
